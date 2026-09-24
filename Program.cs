using GRUPAL.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Render asigna el puerto vía variable de entorno PORT ────────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://+:{port}");

// Add services to the container.
builder.Services.AddControllersWithViews();

// ── Base de datos SQLite (ruta adaptada a producción) ──────────────
var dbPath = builder.Environment.IsProduction()
    ? "/app/data/Altoke.db"
    : "Altoke.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ── Paso 4: Memoria distribuida con Redis ──────────────────────────
var redisConnection = builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrEmpty(redisConnection))
{
    // Producción: usar Redis como caché distribuido
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "AlToke_";
    });
}
else
{
    // Desarrollo / Render sin Redis: caché en memoria
    builder.Services.AddDistributedMemoryCache();
}

// ── Paso 4: Sesiones ───────────────────────────────────────────────
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".AlToke.Session";
});

var app = builder.Build();

// ── Paso 5: Aplicar migraciones automáticamente al iniciar ─────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// El middleware de sesión debe ir ANTES de UseAuthorization
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

