using Microsoft.ML;
using Microsoft.ML.Data;

namespace GRUPAL.Services;

public sealed class SentimientoService(IWebHostEnvironment environment,
    IConfiguration configuration, ILogger<SentimientoService> logger) : IDisposable
{
    private readonly MLContext _ml = new(seed: 42);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private PredictionEngine<EntradaSentimiento, SalidaSentimiento>? _predictor;
    public bool Habilitado => configuration.GetValue("Sentimiento:Enabled", false);

    private string DirectorioModelo => environment.IsProduction()
        ? "/app/data/ml"
        : Path.Combine(environment.ContentRootPath, "ML", "Models");
    private string ModeloPath => Path.Combine(DirectorioModelo, "sentimiento.zip");

    public async Task<(string Etiqueta, double Probabilidad)> EvaluarAsync(string texto)
    {
        if (!Habilitado || string.IsNullOrWhiteSpace(texto)) return ("Sin analizar", 0);
        await _lock.WaitAsync();
        try
        {
            if (_predictor is null && !await CargarOEntrenarAsync()) return ("Sin analizar", 0);
            var res = _predictor!.Predict(new EntradaSentimiento { Comentario = texto });
            return (res.EsPositivo ? "Positivo" : "Negativo", res.EsPositivo ? res.Probabilidad : 1 - res.Probabilidad);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No fue posible evaluar sentimiento.");
            return ("Sin analizar", 0);
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> ReentrenarAsync()
    {
        if (!Habilitado) return false;
        await _lock.WaitAsync();
        try { return await EntrenarAsync(); }
        finally { _lock.Release(); }
    }

    private async Task<bool> CargarOEntrenarAsync()
    {
        if (File.Exists(ModeloPath))
        {
            try
            {
                var modelo = _ml.Model.Load(ModeloPath, out _);
                _predictor = _ml.Model.CreatePredictionEngine<EntradaSentimiento, SalidaSentimiento>(modelo);
                return true;
            }
            catch (Exception ex) { logger.LogWarning(ex, "Modelo anterior no compatible; se reentrenará."); }
        }
        return await EntrenarAsync();
    }

    private async Task<bool> EntrenarAsync()
    {
        var dataset = Path.Combine(environment.ContentRootPath, "ML", "comentarios_entrenamiento.csv");
        if (!File.Exists(dataset)) return false;
        // El CSV heredado etiqueta las clases con 0/1; convertirlas explícitamente evita
        // depender de que el lector de texto interprete esos valores como booleanos.
        var filas = File.ReadLines(dataset).Skip(1)
            .Select(LeerFila)
            .Where(x => x is not null)
            .Cast<DatosSentimiento>()
            .ToList();
        if (filas.Count < 20)
        {
            logger.LogWarning("El dataset de sentimiento tiene {Cantidad} filas válidas; se requieren al menos 20.", filas.Count);
            return false;
        }

        var datos = _ml.Data.LoadFromEnumerable(filas);
        var pipeline = _ml.Transforms.Text.FeaturizeText("Features", nameof(DatosSentimiento.Comentario))
            .Append(_ml.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: "Label", featureColumnName: "Features"));

        var particion = _ml.Data.TrainTestSplit(datos, testFraction: 0.2, seed: 42);
        var modeloEvaluacion = pipeline.Fit(particion.TrainSet);
        var metricas = _ml.BinaryClassification.Evaluate(
            modeloEvaluacion.Transform(particion.TestSet), labelColumnName: "Label");
        logger.LogInformation("Modelo de sentimiento: {Filas} ejemplos; exactitud {Accuracy:P1}; F1 {F1:P1} en partición de evaluación.",
            filas.Count, metricas.Accuracy, metricas.F1Score);

        // Después de medirlo con una partición reservada, se entrena el artefacto final con todos los datos.
        var modelo = pipeline.Fit(datos);
        _predictor?.Dispose();
        _predictor = _ml.Model.CreatePredictionEngine<EntradaSentimiento, SalidaSentimiento>(modelo);
        try
        {
            Directory.CreateDirectory(DirectorioModelo);
            _ml.Model.Save(modelo, datos.Schema, ModeloPath);
        }
        catch (Exception ex) { logger.LogWarning(ex, "El modelo funciona en memoria pero no se pudo guardar en disco."); }
        return true;
    }

    private static DatosSentimiento? LeerFila(string fila)
    {
        var coma = fila.LastIndexOf(',');
        if (coma <= 0) return null;
        var comentario = fila[..coma].Trim();
        if (comentario.Length >= 2 && comentario[0] == '"' && comentario[^1] == '"')
            comentario = comentario[1..^1].Replace("\"\"", "\"", StringComparison.Ordinal);
        var etiqueta = fila[(coma + 1)..].Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(comentario)) return null;
        if (etiqueta is "1" or "true" or "True") return new DatosSentimiento { Comentario = comentario, Label = true };
        if (etiqueta is "0" or "false" or "False") return new DatosSentimiento { Comentario = comentario, Label = false };
        return null;
    }

    public void Dispose()
    {
        _predictor?.Dispose();
        _lock.Dispose();
    }

    public class DatosSentimiento
    {
        [LoadColumn(0)] public string Comentario { get; set; } = "";
        [LoadColumn(1)] public bool Label { get; set; }
    }
    public class EntradaSentimiento { public string Comentario { get; set; } = ""; }
    public class SalidaSentimiento
    {
        [ColumnName("PredictedLabel")] public bool EsPositivo { get; set; }
        [ColumnName("Probability")] public float Probabilidad { get; set; }
    }
}
