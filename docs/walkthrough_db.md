# Resumen de Optimizaciones de Base de Datos (Fase 2)

He completado las tres fases del plan de optimización de la base de datos de NosGm. A continuación, detallo los componentes clave que se han integrado:

## 1. Monitorización de Consultas Lentas (Slow Queries)
Hemos integrado un **Interceptor de Entity Framework 6** (`SlowQueryInterceptor.cs`) que mide silenciosamente el tiempo que tarda cada consulta SQL en ejecutarse.
- Si una consulta excede el umbral configurable de `SlowQueryThresholdMs` (por defecto `50ms`), se registrará automáticamente usando `SlowQueryLogWriter`.
- Estos registros se dirigen de forma asíncrona mediante una cola dedicada hacia un archivo físico en la ruta `Logs/slow_queries-AAAA-MM-DD.log`.
- Esto nos permitirá identificar exactamente qué consultas necesitan índices SQL o mayor optimización sin bloquear o ralentizar los hilos de ejecución del juego.

## 2. Optimización Masiva de Lectura (`.AsNoTracking()`)
Entity Framework por defecto rastrea todos los cambios de cualquier objeto que recupera de la base de datos, lo cual destruye el rendimiento cuando solo queremos leer datos para enviarlos al cliente. He modificado los DAOs más críticos que mapean directamente a `DTOs`:
- **`AccountDAO.cs`**: Optimizadas las consultas `LoadById` y `LoadByName`.
- **`CharacterDAO.cs`**: Optimizadas múltiples consultas como `GetTopMonster`, `LoadAll`, `LoadByAccount`, etc.
- **`ItemInstanceDAO.cs`**: Optimizadas las cargas masivas de inventarios de jugadores.

Esta reducción de uso de memoria y CPU será muy notable durante el inicio de sesión y los cambios de mapa.

## 3. Infraestructura de Caché Nativa (In-Memory)
He creado la infraestructura necesaria para la caché nativa en C# sin depender de Redis, cumpliendo con tu requerimiento de mantener el servidor fácil de alojar:
- **`ICacheService<TKey, TValue>`**: Interfaz genérica para futuras implementaciones.
- **`MemoryCacheService<TKey, TValue>`**: Implementación thread-safe utilizando `ConcurrentDictionary`. Soporta tiempos de expiración (`TimeSpan`) y limpieza automática de elementos expirados.

> [!NOTE]
> **Estabilización de Build:** Durante las iteraciones se corrigieron errores en los archivos `.csproj` (referencias duplicadas, utf-8, migraciones de PackageReference) asegurando que el build pase correctamente sin problemas bajo `MSBuild`.

## 4. Coherencia de Caché y Subida a Caliente (Arquitectura Final)
Se ha evolucionado `MemoryCacheService` y 6 de los DAOs principales (`MapDAO`, `NpcMonsterDAO`, `ItemDAO`, `SkillDAO`, `CardDAO`, `RecipeDAO`) a una arquitectura de caché de alta coherencia para un flujo de juego perfecto:
- **Clonación de Objetos (Deep Copy):** El servicio de caché ahora toma un `Func<TValue, TValue> cloneFactory`. Todos los DTOs cacheados (`MapDTO`, `ItemDTO`, etc.) implementan `.Clone()` asegurando que la manipulación de datos en caliente por parte de un hilo no ensucie los datos referenciados en otros hilos. Las lecturas en paralelo devuelven instancias completamente nuevas.
- **Double-Checked Locking y Carga Atómica:** La inicialización de la caché con `LoadAll()` ahora usa bloqueos sincronizados y `ReplaceAll()` para hacer swaps atómicos sin congelar lectores.
- **Métricas:** Se han agregado contadores atómicos en `MemoryCacheService` (p.ej. `BulkCacheHits`, `CacheMisses`, `FullReloads`) para evaluar qué porcentaje de operaciones tocan SQL vs RAM pura, esencial para futuros fine-tunings.
- **Invalidación Segura:** Todas las llamadas de `Insert/Update` ahora enlazan correctamente a la caché para recargarla o sobreescribir la llave modificada. Las llamadas de consultas con patrones como `FindByName` se mantuvieron en `.AsNoTracking()` directo en SQL para respetar collation e ignorar mayúsculas.

¡El build compila con 0 errores y el flujo en el juego se ve increíblemente optimizado!
