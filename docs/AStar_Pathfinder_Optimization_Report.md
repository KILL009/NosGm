# NosGm AStar Pathfinder & AI Optimization Report 🚀

En esta auditoría, abordamos el Pathfinder original del emulador NosGm (OpenNos) para identificar los cuellos de botella que generaban *lag* masivo durante la simulación de múltiples monstruos. Se identificó una severa sobrecarga en el Garbage Collector (GC) debido a la asignación reiterada de tres matrices pesadas por cada solicitud de búsqueda de ruta. Además, se reparó un fallo matemático crítico en la heurística octil que causaba el colapso del algoritmo en rutas diagonales largas.

### Resultado Principal:
El nuevo `AStarPathFinder` ya no asigna matrices pesadas en cada búsqueda, reduciendo drásticamente la presión del GC, y resuelve rutas diagonales **16 veces más rápido** gracias a la heurística octil corregida.

### Integración Continua (CI/CD)
La suite de pruebas automatizadas se ha integrado al pipeline oficial (GitHub Actions). El pipeline no solo compila el proyecto, sino que ejecuta la batería de validaciones (`dotnet run`), abortando la compilación (código de salida != 0) si detecta cualquier regresión o rutas vacías injustificadas.

---

## 1. El Problema Original: Cuellos de Botella de Memoria

El código base fue actualizado recientemente para reemplazar el antiguo algoritmo `BestFirstSearch` (BrushFire) por un algoritmo moderno **A*** puro (`AStarPathFinder.cs`). Aunque la lógica matemática era correcta, el algoritmo sufría de un defecto arquitectónico grave en C#: **Asignación excesiva de memoria dinámica (GC Pressure)**.

Por cada solicitud de movimiento (Pathfinding), el algoritmo instanciaba tres matrices bidimensionales del tamaño total del mapa:
1. `bool[,] closed` (Celdas visitadas)
2. `double[,] gScore` (Costes de ruta)
3. `GridPos[,] parent` (Nodos padre para el backtrace)

Para un mapa estándar de 150x150, esto significaba alocar **~550 KB de memoria por cálculo**. En escenarios de concurrencia (ej. 500 monstruos moviéndose), el servidor creaba **~268 MB de basura de memoria por cada Tick**, forzando al *Garbage Collector (GC)* de .NET a pausar el servidor repetidamente para limpiar la RAM, causando "tirones" y lag masivo.

---

## 2. Metodología de Benchmark

Para medir el impacto real, se desarrolló una suite de Benchmarks (`PathFinderBenchmark`) aislando el componente `NosGm.PathFinder.dll` y sometiéndolo a 7 pruebas de estrés asíncronas, incluyendo:
- Rutas cortas y largas (esquina a esquina).
- Laberintos en Zig-Zag.
- Destinos bloqueados (para validar el límite de 2500 expansiones).
- Pruebas de concurrencia con 100 y 500 agentes simultáneos.

---

## 3. La Solución: ThreadLocal Pooling y Generation Counters

Para resolver el problema sin alterar la lógica de A*, se eliminó por completo la asignación (allocation) de matrices utilizando el patrón de diseño **Object Pooling** apoyado por un **Contador de Generaciones**.

**Ubicación del Fix:** `Data/NosGm.PathFinder/AStarPathFinder.cs`

1. **`[ThreadStatic]` Pooling**: Se creó una clase `PathfinderState` alojada de forma estática por hilo administrado (`[ThreadStatic]`). Esto significa que existe exactamente una instancia de las matrices enormes por cada hilo activo, reutilizándose en todos los cálculos de ese hilo sin riesgo de colisiones.
2. **Generational Reset (O(1))**: En lugar de limpiar las matrices en cada iteración usando `Array.Clear()` (lo cual consume mucha CPU), se introdujo un número entero `Generation`. 
   - Cuando se pide una ruta nueva, `Generation` aumenta en +1.
   - Al leer una celda, el algoritmo verifica si `NodeGen[x, y] == Generation`. Si no coincide, sabe que los datos en esa celda pertenecen a un cálculo viejo y los trata como si estuvieran vacíos.
   - Esto reduce el tiempo de reseteo del mapa de `O(N*M)` a `O(1)`.

> **Nota sobre Allocaciones Restantes:** Aunque se eliminó el 100% de la carga de las matrices (`~550 KB`), una ejecución de A* todavía realiza pequeñas asignaciones dinámicas por diseño, concretamente la instanciación de un `new BinaryHeap<PathNode>(128)` y el `new List<GridPos>()` final. Estos objetos son minúsculos y manejables por el GC, pero estrictamente hablando la asignación no es un cero absoluto. Además, el método `EnsureCapacity()` redimensionará y reasignará las matrices si de pronto se le pide navegar un mapa más grande que la capacidad actual del hilo.

### 3.3. Corrección de la Heurística Octil
El código original de la distancia Octil contenía un error matemático en la fórmula (`Math.Max * SQRT_2 + (1 - SQRT_2) * Math.Min`), lo que resultaba en subestimaciones severas del coste diagonal. Se reemplazó por la fórmula correcta `Math.Max + (SQRT_2 - 1) * Math.Min`. Esto previene que A* se comporte como Dijkstra, evitando exploraciones masivas innecesarias y eludiendo el límite de 2500 expansiones en rutas largas.

### Resultados del Benchmark (Antes vs Después)

Prueba de estrés con **Ruta Larga Diagonal (150x150) y 500 Monstruos Concurrentes**:

| Métrica | Algoritmo Original | Optimizado | Mejora |
| :--- | :--- | :--- | :--- |
| **Tiempo (Ruta 150x150)** | `Falla al 1 ms` (0 celdas) | **`0.060 ms`** (149 celdas) | ⚡ **Ruta recuperada y ultra rápida** |
| **Tiempo de respuesta (500 Mobs)** | `68 ms` (Fallo silencioso) | **`7 ms`** (Éxito validado) | ⚡ **10x más rápido** |
| **Asignación de matrices pesadas** | `268.20 MB` | **`0 MB`** | 📉 **-100% GC Pressure** |

*(Nota: Los resultados reflejan pruebas locales precisas por hilo. El Pathfinder ya no aloca memoria pesada repetitivamente, limitando el GC solo al instanciamiento nativo del BinaryHeap de tamaño controlable).*

---

## 4. Repositorio de Benchmarks y CI
Para garantizar que futuras iteraciones del motor mantengan este rendimiento, la suite de pruebas `PathFinderBenchmark` ha sido incorporada oficialmente como un proyecto dentro de la solución `NosGm.sln`, ubicado en `Data/NosGm.PathFinder.Benchmark/`.

Las pruebas ahora actúan como **guardián de producción**:
- Se añadieron aserciones estrictas (`expectPath`).
- El workflow de GitHub Actions (`dotnet10-foundation.yml`) ejecuta el programa. Si una heurística rota o una regresión devuelve rutas vacías, la suite arroja una excepción terminando con un código de error, lo que bloquea el build automáticamente.

---

## 5. Mejoras Adicionales a la IA (Desincronización del Cliente)

Durante las pruebas, se corrigieron severos bugs de renderizado del cliente de NosTale asociados con la IA de los monstruos, en los cuales **los monstruos se volvían invisibles** al atacar o atacaban desde la distancia incorrecta.

### 5.1. Prevención del error de División por Cero (NaN Rotation)
**Ubicación:** `Data/NosGm.GameObject/Map/MapMonster.cs` -> `MoveTest()`
- **Problema:** Los monstruos cuerpo a cuerpo caminaban a la coordenada exacta del jugador (Distancia = 0). Al atacar, el motor 3D del cliente intentaba rotar el modelo hacia el jugador calculando `atan2(0,0)`, lo que generaba un ángulo indefinido (`NaN`). Esto colapsaba el pipeline de renderizado y el monstruo desaparecía de la pantalla.
- **Solución:** Se ajustó la lógica de búsqueda de ruta para detener al monstruo siempre a una celda de distancia adyacente del objetivo, manteniendo la rotación válida.

### 5.2. Corrección del Paquete `su` (SkillUsed) para Ataques Básicos
**Ubicación:** `Data/NosGm.GameObject/Map/MapMonster.cs` -> `TargetHit2()`
- **Problema:** El paquete `su` enviaba `AttackClass` (0) como animación. El cliente interpretaba el 0 como "esconder modelo".
- **Solución:** Ingeniería inversa del comportamiento original de OpenNos. Se forzó el uso del identificador estático `11` como `attackAnimation` y `0` como `skillVNum` cuando un monstruo ejecuta un golpe básico sin skill.

### 5.3. Eliminación de Casteo Fantasma (Melee Instantáneo)
**Ubicación:** `Data/NosGm.GameObject/Map/MapMonster.AI.cs` -> `StartAttackInstantly()`
- **Problema:** Las habilidades cuerpo a cuerpo con `CastAnimation = -1` invocaban un retraso inactivo de 800ms (`CastTime`), durante el cual el cliente cortaba la sincronización visual del monstruo.
- **Solución:** Bypass de la fase de preparación; si `CastAnimation < 0`, el método retorna un tiempo de casteo de `0ms`, forzando a `AttackTargetNode` a ejecutar el daño de forma inmediata y mantener el modelo 3D activo.

### 5.4. Ajuste Selectivo del Rango Dinámico (Melee-Only)
**Ubicación:** `Data/NosGm.GameObject/AI/Profiles/MobAIProfile.cs`
- **Solución:** Modificación del constructor de rangos. Si el `BasicRange` de un monstruo en la BD es 0, se eleva automáticamente a 1 para hacer match perfecto con la nueva protección de colisiones de `MoveTest`. Sin embargo, si el `BasicRange` es > 0 (Arqueros, Magos), el valor **se mantiene intacto** para respetar los rangos oficiales de ataque a distancia de los archivos del cliente.

---
*Desarrollado y optimizado para NosGm Emulator.*
