using System.Net.Http.Json;
using System.Text.Json;
using GRUPAL.Data;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Services;

/// <summary>Índice Algolia opcional de avisos públicos. Sin claves, se utiliza SQLite.</summary>
public class AlgoliaObjetosService(IHttpClientFactory factory, IConfiguration settings, ApplicationDbContext db,
    ILogger<AlgoliaObjetosService> logger)
{
    private string AppId => settings["Algolia:ApplicationId"] ?? "";
    private string Index => Uri.EscapeDataString(settings["Algolia:IndexName"] ?? "altoke_objetos");
    private string SearchKey => settings["Algolia:SearchApiKey"] ?? "";
    private string AdminKey => settings["Algolia:AdminApiKey"] ?? "";
    public bool PuedeBuscar => AppId.Length > 0 && SearchKey.Length > 0;
    public bool PuedeSincronizar => AppId.Length > 0 && AdminKey.Length > 0;

    public async Task<List<object>?> BuscarAsync(string termino, CancellationToken ct)
    {
        if (!PuedeBuscar) return null;
        try
        {
            using var client = Cliente(SearchKey, true);
            using var result = await client.PostAsJsonAsync($"1/indexes/{Index}/query",
                new { query = termino, hitsPerPage = 6, attributesToRetrieve = new[] { "objectID", "titulo", "categoria", "ubicacion" } }, ct);
            result.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync(ct));
            var hits = json.RootElement.GetProperty("hits");
            return hits.EnumerateArray().Select(x => (object)new
            {
                id = int.TryParse(x.GetProperty("objectID").GetString(), out var i) ? i : 0,
                titulo = x.GetProperty("titulo").GetString(),
                categoria = x.GetProperty("categoria").GetString(),
                ubicacion = x.GetProperty("ubicacion").GetString()
            }).ToList();
        }
        catch (Exception ex) { logger.LogWarning(ex, "Algolia no respondió; se usa búsqueda local."); return null; }
    }

    public async Task<int> SincronizarAsync(CancellationToken ct)
    {
        if (!PuedeSincronizar) throw new InvalidOperationException("Configura Algolia antes de sincronizar.");
        var objetos = await db.ObjetosPerdidos.AsNoTracking().ToListAsync(ct);
        using var client = Cliente(AdminKey, false);
        // Se limpia el índice primero para quitar avisos eliminados en AlToke.
        using var limpiar = await client.PostAsJsonAsync($"1/indexes/{Index}/clear", new { }, ct);
        limpiar.EnsureSuccessStatusCode();
        foreach (var lote in objetos.Chunk(100))
        {
            var requests = lote.Select(o => new
            {
                action = "addObject",
                body = new { objectID = o.Id.ToString(), titulo = o.Título, categoria = o.Categoría,
                    descripcion = o.Descripcion, ubicacion = o.Ubicacion, estado = o.Estado.ToString(), fecha = o.Fecha }
            });
            using var response = await client.PostAsJsonAsync($"1/indexes/{Index}/batch", new { requests }, ct);
            response.EnsureSuccessStatusCode();
        }
        return objetos.Count;
    }

    private HttpClient Cliente(string key, bool read)
    {
        var c = factory.CreateClient();
        c.Timeout = TimeSpan.FromSeconds(10);
        c.BaseAddress = new Uri($"https://{AppId}{(read ? "-dsn" : "")}.algolia.net/");
        c.DefaultRequestHeaders.Add("X-Algolia-Application-Id", AppId);
        c.DefaultRequestHeaders.Add("X-Algolia-API-Key", key);
        return c;
    }
}
