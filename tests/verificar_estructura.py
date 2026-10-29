"""Validación de estructura de AlToke. No reemplaza dotnet build ni pruebas de integración."""
from pathlib import Path
import json
import sqlite3
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
files = [
    'Controllers/ExplorarController.cs', 'Controllers/AsistenteController.cs',
    'Controllers/FavoritosController.cs', 'Controllers/RecuperacionesController.cs',
    'Controllers/ObjetosApiController.cs', 'Controllers/ReportesController.cs',
    'Controllers/ConocimientoController.cs',
    'Controllers/ObjetosPerdidosController.cs', 'Controllers/AsistenteController.cs',
    'Views/Explorar/Detalle.cshtml', 'Views/Favoritos/Index.cshtml',
    'Views/Recuperaciones/Index.cshtml', 'Views/Reportes/Contactos.cshtml',
    'Views/Home/Contacto.cshtml', 'Views/Shared/_AsistenteChat.cshtml',
    'Views/ObjetosPerdidos/Editar.cshtml', 'Views/Conocimiento/Index.cshtml',
    'Views/Favoritos/Index.cshtml', 'Services/AlgoliaObjetosService.cs',
    'wwwroot/js/busqueda.js', 'wwwroot/js/asistente.js',
    'Migrations/20261028000000_FuncionesDemoAlToke.cs', 'ML/comentarios_entrenamiento.csv',
]
for name in files:
    assert (root / name).is_file(), f'Falta: {name}'

cfg = json.loads((root / 'appsettings.json').read_text())
assert not cfg['Cloudinary']['ApiSecret'], 'No debe haber una clave Cloudinary en el ZIP'
assert not cfg['AdminSettings']['Password'], 'No debe quedar la contraseña admin en el ZIP'
assert not cfg['Groq']['ApiKey'], 'No debe quedar la clave Groq en el ZIP'
assert not cfg['Algolia']['AdminApiKey'], 'No debe quedar la clave admin de Algolia en el ZIP'
ET.parse(root / 'GRUPAL.csproj')

db_path = root / 'Altoke.db'
if db_path.is_file() and db_path.stat().st_size:
    # Abrir solo lectura: nunca crear, modificar ni consultar datos de usuarios.
    uri = f'file:{db_path.as_posix()}?mode=ro'
    con = sqlite3.connect(uri, uri=True)
    tables = {row[0] for row in con.execute("SELECT name FROM sqlite_master WHERE type='table'")}
    assert {'Usuarios', 'ObjetosPerdidos'} <= tables, 'SQLite no contiene las tablas base esperadas'
    migrations = {row[0] for row in con.execute('SELECT MigrationId FROM __EFMigrationsHistory')}
    assert '20260923153530_Inicial' in migrations, 'Falta la migración inicial'
    con.close()
    db_note = 'esquema SQLite base verificado en modo lectura'
else:
    db_note = 'SQLite no disponible; comprobación de base de datos omitida'
print(f'Validación estática OK; {len(files)} componentes comprobados; {db_note}.')
print('Nota: Compilar y ejecutar con dotnet build, dotnet run y el flujo UI completo en .NET 10.')
