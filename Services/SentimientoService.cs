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

    public async Task<(string Etiqueta, double Probabilidad)> EvaluarAsync(string texto)
    {
        if (!Habilitado || string.IsNullOrWhiteSpace(texto)) return ("Sin analizar", 0);
        await _lock.WaitAsync();
        try
        {
            if (_predictor is null && !CargarOEntrenar()) return ("Sin analizar", 0);
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
        try { return Entrenar(); }
        finally { _lock.Release(); }
    }

    private string ModeloPath => Path.Combine(environment.ContentRootPath, "ML", "modeloSentimientoAlToke.zip");

    private bool CargarOEntrenar()
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
        return Entrenar();
    }

    private bool Entrenar()
    {
        var dataset = Path.Combine(environment.ContentRootPath, "ML", "comentarios_entrenamiento.csv");
        if (!File.Exists(dataset)) return false;
        var datos = _ml.Data.LoadFromTextFile<DatosSentimiento>(dataset, separatorChar: ',', hasHeader: true, allowQuoting: true);
        var pipeline = _ml.Transforms.Text.FeaturizeText("Features", nameof(DatosSentimiento.Comentario))
            .Append(_ml.BinaryClassification.Trainers.LbfgsLogisticRegression("Label", "Features"));
        var modelo = pipeline.Fit(datos);
        _predictor = _ml.Model.CreatePredictionEngine<EntradaSentimiento, SalidaSentimiento>(modelo);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ModeloPath)!);
            _ml.Model.Save(modelo, datos.Schema, ModeloPath);
        }
        catch (Exception ex) { logger.LogWarning(ex, "El modelo funciona en memoria pero no se pudo guardar en disco."); }
        return true;
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
