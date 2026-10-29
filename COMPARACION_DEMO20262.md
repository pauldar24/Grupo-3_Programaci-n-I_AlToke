# Comparación técnica: Demo20262 y Grupo 3 – AlToke

El repositorio Demo20262 es una tienda digital de mascotas. El Grupo 3 es una plataforma de objetos perdidos para la comunidad USMP. La paridad apropiada consiste en trasladar los **patrones funcionales** aplicables a AlToke, no el catálogo de mascotas, sus pedidos ni sus datos de entrenamiento de recomendaciones.

## Incorporaciones en Grupo 3

1. Se creó una API JSON de publicaciones con lectura pública, escritura por propietario y Swagger en desarrollo.
2. Se mantuvo el explorador y se añadió paginación, búsqueda por ubicación, sugerencias locales y Algolia opcional.
3. Cada aviso tiene una página de detalle, flujo de favoritos y recomendaciones contextuales.
4. Los miembros pueden solicitar una recuperación al autor y recibir una respuesta privada; el estado se refleja en el perfil y en un contador de avisos.
5. Existe una sección de Contacto con almacenamiento, panel de gestión y análisis opcional por un modelo ML.NET entrenado en comentarios.
6. El asistente contextual de AlToke usa el contenido público y respuestas administrables y documentos TXT/MD de una base local; puede generar la respuesta vía Groq si se configura.
7. Se implementó una migración aditiva para las cuatro tablas nuevas, conservando las tablas y datos de usuarios y objetos originales.
8. Se retiraron secretos del código de configuración y se incorporó PBKDF2 para cuentas nuevas y actualización gradual de contraseñas heredadas.
9. Se mantuvo el CSS principal, la navegación, la identidad naranja y las páginas originales.

## No es paridad de infraestructura completa

- Sin Qdrant/embeddings ni Semantic Kernel: base FAQ local, con Groq como generación opcional.
- Sin recomendador matricial ML.NET para objetos: los datos de ventas del Demo no son transferibles. Recomendaciones locales por afinidad, sin depender de un historial artificial de calificaciones.
- Sin RabbitMQ/CloudAMQP ni PieSocket: la recuperación usa transacciones SQLite y consulta periódica de cambios de estado.
- Sin adopción completa de ASP.NET Identity: permanece el esquema de usuarios de Grupo 3 para preservar la base; se refuerza el almacenamiento de contraseñas.
- El cliente de DummyJSON y el procesamiento de pedidos/stock no aplican al ámbito de AlToke.

Para alcanzar equivalencia estricta en todas las tecnologías, habría que diseñar despliegues y credenciales de Qdrant, AMQP, PieSocket, las políticas de usuarios para migrar a Identity y un dataset de interacciones reales para entrenar recomendaciones, además de ejecutar una batería de pruebas con .NET 10. Esos requisitos no se consideran implementados en esta entrega.
