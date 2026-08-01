# NosGm AStar Pathfinder & AI Optimization Report 🚀

Este documento detalla la auditoría, refactorización y optimización del nuevo sistema de búsqueda de rutas (`AStarPathFinder`) y la Inteligencia Artificial (IA) de los monstruos en el emulador NosGm.

---

## 1. El Problema Original: Cuellos de Botella de Memoria

El código base fue actualizado recientemente para reemplazar el antiguo algoritmo `BestFirstSearch` (BrushFire) por un algoritmo moderno **A*** puro (`AStarPathFinder.cs`). Aunque la lógica matemática era correcta, el algoritmo sufría de un defecto arquitectónico grave en C#: **Asignación excesiva de memoria dinámica (GC Pressure)**.

Por cada solicitud de movimiento (Pathfinding), el algoritmo instanciaba tres matrices bidimensionales del tamaño total del mapa:

1. `bool[,] closed` (celdas visitadas).
2. `double[,] gScore` (costes de ruta).
3. `GridPos[,] parent` (nodos padre para el backtrace).

Para un mapa estándar de 150x150, esto significaba asignar aproximadamente **550 KB de memoria por cálculo**. En escenarios de concurrencia, por ejemplo 500 monstruos moviéndose, el servidor creaba aproximadamente **268 MB de basura de memoria por cada tick**, forzando al *Garbage Collector* de .NET a pausar el servidor repetidamente y causando tirones y lag.

---

## 2. Metodología de Benchmark

Para medir el impacto real se desarrolló una suite de Benchmarks (`PathFinderBenchmark`) que utiliza el componente productivo `NosGm.PathFinder.dll` y lo somete a siete pruebas de estrés asíncronas, incluyendo:

- Rutas cortas y largas, de esquina a esquina.
- Laberintos en zigzag.
- Destinos bloqueados, para validar el límite de 2,500 expansiones.
- Pruebas de concurrencia con 100 y 500 agentes simultáneos.

---

## 3. La Solución: Thread-Local Pooling y Generation Counters

Para resolver el problema sin alterar la lógica de A*, se eliminó la asignación repetitiva de las matrices pesadas mediante **Object Pooling** apoyado por un **contador de generaciones**.

**Ubicación del arreglo:** `Data/NosGm.PathFinder/AStarPathFinder.cs`

1. **`[ThreadStatic]` Pooling**: se creó una clase `PathfinderState` alojada de forma estática por hilo administrado. Existe una instancia de las matrices por cada hilo administrado que utiliza el Pathfinder, y esa instancia se reutiliza en los cálculos posteriores del mismo hilo.
2. **Generational Reset, O(1)**: en lugar de limpiar las matrices en cada iteración con `Array.Clear()`, se introdujo un entero `Generation`.
   - Cuando se solicita una ruta nueva, `Generation` aumenta en uno.
   - Al leer una celda, el algoritmo comprueba si `NodeGen[x, y] == Generation`.
   - Si no coincide, los datos de esa celda pertenecen a un cálculo anterior y se tratan como vacíos.
   - Esto reduce el coste de reinicio del mapa de `O(N*M)` a `O(1)`.

> **Nota sobre las asignaciones restantes:** aunque se eliminó la carga repetitiva de las matrices pesadas, una ejecución de A* todavía realiza pequeñas asignaciones dinámicas por diseño, concretamente `new BinaryHeap<PathNode>(128)` y la `List<GridPos>` devuelta. Además, `EnsureCapacity()` reasigna las matrices cuando se necesita navegar un mapa mayor que la capacidad actual del hilo. Por tanto, la mejora de `0 MB` se refiere a la asignación repetitiva de matrices pesadas, no a cero bytes absolutos durante toda la operación.

### Resultados del Benchmark, antes y después

Prueba de estrés con **500 monstruos concurrentes**:

| Métrica | Algoritmo original | Optimizado con pooling | Mejora |
| :--- | :--- | :--- | :--- |
| **Tiempo de respuesta** | `137 ms` | **`68 ms`** | ⚡ **50% más rápido** |
| **Asignación de matrices pesadas** | `268.20 MB` | **`0 MB`** | 📉 **-100% de presión por matrices** |

Los aproximadamente 5 MB residuales observados en algunas ejecuciones provienen principalmente del trabajo asíncrono de `Task.Run`, el `BinaryHeap` y las listas retornadas.

---

## 4. Repositorio de Benchmarks

La suite `PathFinderBenchmark` forma parte de `NosGm.sln` y está ubicada en:

```text
Data/NosGm.PathFinder.Benchmark/
```

El proyecto usa un `ProjectReference` real hacia:

```text
Data/NosGm.PathFinder/NosGm.PathFinder.csproj
```

No mantiene copias locales del algoritmo, por lo que siempre prueba el motor productivo utilizado por NosGm. El comando directo de compilación también se ejecuta en CI:

```powershell
dotnet build .\Data\NosGm.PathFinder.Benchmark\PathFinderBenchmark.csproj -c Release
```

---

## 5. Mejoras Adicionales a la IA y Sincronización del Cliente

Durante las pruebas se corrigieron errores de renderizado del cliente de NosTale asociados con la IA de los monstruos, en los cuales algunos monstruos desaparecían al atacar o utilizaban distancias incorrectas.

### 5.1. Prevención de distancia cero y rotación indefinida

**Ubicación:** `Data/NosGm.GameObject/Map/MapMonster.cs`, método `MoveTest()`.

- **Problema:** algunos monstruos cuerpo a cuerpo caminaban hasta la coordenada exacta del jugador. Una distancia de cero podía producir una rotación indefinida al calcular la orientación del modelo.
- **Solución:** la ruta se detiene en una celda adyacente al objetivo, manteniendo una orientación y una separación válidas.

### 5.2. Corrección del paquete `su` para ataques básicos

**Ubicación:** `Data/NosGm.GameObject/Map/MapMonster.cs`, método `TargetHit2()`.

- **Problema:** el paquete `su` utilizaba `AttackClass` como animación y podía enviar `0`, valor que el cliente interpretaba incorrectamente.
- **Solución:** los golpes básicos sin skill utilizan `11` como `attackAnimation` y `0` como `skillVNum`.

### 5.3. Eliminación de casteo fantasma en ataques instantáneos

**Ubicación:** `Data/NosGm.GameObject/Map/MapMonster.AI.cs`, método `StartAttackInstantly()`.

- **Problema:** habilidades con `CastAnimation < 0` podían conservar un retraso de casteo de 800 ms, provocando desincronización visual.
- **Solución:** cuando `CastAnimation < 0`, la preparación utiliza un tiempo de casteo de `0 ms` y el ataque continúa inmediatamente.

### 5.4. Ajuste selectivo del rango, exclusivo para melee

**Ubicación:** `Data/NosGm.GameObject/AI/Profiles/MobAIProfile.cs`.

La regla final es:

```csharp
monster.Monster.BasicRange <= 0
    ? 1
    : monster.Monster.BasicRange;
```

Si `BasicRange` es cero o negativo, se eleva a uno para que el monstruo cuerpo a cuerpo pueda atacar desde una celda adyacente. Los monstruos con rango positivo, incluidos arqueros y magos, conservan exactamente el valor definido en la base de datos.

---

*Desarrollado y optimizado para NosGm Emulator.*
