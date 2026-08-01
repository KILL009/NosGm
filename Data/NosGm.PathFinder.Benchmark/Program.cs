using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using NosGm.PathFinder;

namespace NosGm.PathFinder.Benchmark
{
    class Program
    {
        // =====================================================================
        //  UTILIDADES
        // =====================================================================

        static GridPos[][] BuildGrid(int sizeX, int sizeY, Action<GridPos[][]> configure = null)
        {
            var grid = new GridPos[sizeX][];
            for (int x = 0; x < sizeX; x++)
            {
                grid[x] = new GridPos[sizeY];
                for (int y = 0; y < sizeY; y++)
                    grid[x][y] = new GridPos { X = (short)x, Y = (short)y, Value = 0 }; // 0 = walkable
            }
            configure?.Invoke(grid);
            return grid;
        }

        static void SetWall(GridPos[][] grid, int x, int y) => grid[x][y].Value = 1; // 1 = blocked

        static (long ElapsedMs, long MemBefore, long MemAfter, int PathLen) RunTest(
            string name, GridPos[][] grid, GridPos start, GridPos end, int repetitions = 1)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long memBefore = GC.GetTotalMemory(false);
            var sw = Stopwatch.StartNew();

            List<GridPos> result = null;
            for (int i = 0; i < repetitions; i++)
                result = AStarPathFinder.FindPath(start, end, grid);

            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"  [{name}]");
            Console.WriteLine($"    Repeticiones : {repetitions}");
            Console.WriteLine($"    Tiempo total : {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"    Tiempo/iter  : {(double)sw.ElapsedMilliseconds / repetitions:F3} ms");
            Console.WriteLine($"    Longitud ruta: {result?.Count ?? 0} celdas");
            Console.WriteLine($"    ΔMem (bytes) : {(memAfter - memBefore):+#;-#;0}");
            Console.WriteLine();

            return (sw.ElapsedMilliseconds, memBefore, memAfter, result?.Count ?? 0);
        }

        static async Task<(long ElapsedMs, long MemBefore, long MemAfter)> RunConcurrentTest(
            string name, GridPos[][] grid, GridPos start, GridPos end, int monsterCount)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long memBefore = GC.GetTotalMemory(false);
            var sw = Stopwatch.StartNew();

            var tasks = new Task[monsterCount];
            for (int i = 0; i < monsterCount; i++)
                tasks[i] = Task.Run(() => AStarPathFinder.FindPath(start, end, grid));

            await Task.WhenAll(tasks);
            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"  [{name}] — {monsterCount} monstruos concurrentes");
            Console.WriteLine($"    Tiempo total : {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"    ΔMem (bytes) : {(memAfter - memBefore):+#;-#;0}  ({(memAfter - memBefore) / 1024.0 / 1024.0:F2} MB)");
            Console.WriteLine();

            return (sw.ElapsedMilliseconds, memBefore, memAfter);
        }

        // =====================================================================
        //  MAIN
        // =====================================================================
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("          NosGm — AStarPathFinder Benchmark Suite          ");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();

            // -----------------------------------------------------------------
            // TEST 1: Trayecto corto y limpio (10 celdas, línea recta)
            // -----------------------------------------------------------------
            Console.WriteLine("▶  TEST 1: Trayecto Corto — 10 celdas, línea recta");
            {
                var grid = BuildGrid(50, 50);
                var start = grid[5][5];
                var end   = grid[5][15];
                RunTest("Ruta corta 10 celdas", grid, start, end, repetitions: 1000);
            }

            // -----------------------------------------------------------------
            // TEST 2: Trayecto largo — 150x150 de esquina a esquina
            // -----------------------------------------------------------------
            Console.WriteLine("▶  TEST 2: Trayecto Largo — 150x150, esquina a esquina");
            {
                var grid = BuildGrid(150, 150);
                var start = grid[0][0];
                var end   = grid[149][149];
                RunTest("Ruta larga 150×150", grid, start, end, repetitions: 100);
            }

            // -----------------------------------------------------------------
            // TEST 3: Destino imposible — celda encerrada (límite de expansiones)
            // -----------------------------------------------------------------
            Console.WriteLine("▶  TEST 3: Destino Imposible — celda encerrada (hit 2500 expansions limit)");
            {
                var grid = BuildGrid(100, 100);
                // Encerrar la celda (50,50) con paredes
                SetWall(grid, 49, 49); SetWall(grid, 49, 50); SetWall(grid, 49, 51);
                SetWall(grid, 50, 49);                         SetWall(grid, 50, 51);
                SetWall(grid, 51, 49); SetWall(grid, 51, 50); SetWall(grid, 51, 51);
                var start = grid[0][0];
                var end   = grid[50][50];
                RunTest("Destino imposible (encerrado)", grid, start, end, repetitions: 10);
            }

            // -----------------------------------------------------------------
            // TEST 4: Pasillo estrecho en Zig-Zag (laberinto)
            // -----------------------------------------------------------------
            Console.WriteLine("▶  TEST 4: Pasillo Estrecho — Zig-Zag (laberinto)");
            {
                int W = 80, H = 80;
                var grid = BuildGrid(W, H);

                // Crear laberinto en serpentina: paredes horizontales con huecos alternados
                for (int row = 4; row < H - 4; row += 8)
                {
                    for (int col = 0; col < W - 2; col++)
                        SetWall(grid, col, row);
                    // Hueco en el lado derecho en filas pares, izquierdo en impares
                    int gap = ((row / 8) % 2 == 0) ? W - 2 : 1;
                    for (int g = gap; g < gap + 2 && g < W; g++)
                        grid[g][row].Value = 0;
                }

                var start = grid[1][1];
                var end   = grid[W - 2][H - 2];
                RunTest("Zig-Zag laberinto 80×80", grid, start, end, repetitions: 200);
            }

            // -----------------------------------------------------------------
            // TEST 5a: 100 monstruos concurrentes
            // -----------------------------------------------------------------
            Console.WriteLine("▶  TEST 5a: Concurrencia — 100 monstruos simultáneos");
            {
                var grid = BuildGrid(150, 150);
                var start = grid[0][0];
                var end   = grid[149][149];
                await RunConcurrentTest("100 monstruos", grid, start, end, 100);
            }

            // -----------------------------------------------------------------
            // TEST 5b: 500 monstruos concurrentes
            // -----------------------------------------------------------------
            Console.WriteLine("▶  TEST 5b: Concurrencia — 500 monstruos simultáneos");
            {
                var grid = BuildGrid(150, 150);
                var start = grid[0][0];
                var end   = grid[149][149];
                await RunConcurrentTest("500 monstruos", grid, start, end, 500);
            }

            // -----------------------------------------------------------------
            // TEST 6: Cancelación rápida (objetivo ya alcanzado = start == end)
            // -----------------------------------------------------------------
            Console.WriteLine("▶  TEST 6: Cancelación Rápida — start == end (ya en destino)");
            {
                var grid = BuildGrid(150, 150);
                var pos  = grid[75][75];
                RunTest("Start == End (ya en destino)", grid, pos, pos, repetitions: 10000);
            }

            // -----------------------------------------------------------------
            // TEST 7: Objetivo en movimiento — recalculo cada 500ms × 20 veces
            // -----------------------------------------------------------------
            Console.WriteLine("▶  TEST 7: Objetivo en Movimiento — 20 recalculos seguidos");
            {
                var grid   = BuildGrid(150, 150);
                var start  = grid[0][0];
                var rnd    = new Random(42);
                var sw     = Stopwatch.StartNew();
                long memBefore = GC.GetTotalMemory(true);
                int totalCells = 0;

                for (int i = 0; i < 20; i++)
                {
                    var end = grid[rnd.Next(100, 149)][rnd.Next(100, 149)];
                    var path = AStarPathFinder.FindPath(start, end, grid);
                    totalCells += path.Count;
                }

                sw.Stop();
                long memAfter = GC.GetTotalMemory(false);
                Console.WriteLine($"  [Objetivo en movimiento — 20 recalculos]");
                Console.WriteLine($"    Tiempo total : {sw.ElapsedMilliseconds} ms");
                Console.WriteLine($"    Tiempo/iter  : {(double)sw.ElapsedMilliseconds / 20:F1} ms");
                Console.WriteLine($"    Celdas total : {totalCells}");
                Console.WriteLine($"    ΔMem (bytes) : {(memAfter - memBefore):+#;-#;0}  ({(memAfter - memBefore) / 1024.0:F1} KB)");
                Console.WriteLine();
            }

            // -----------------------------------------------------------------
            // RESUMEN DIAGNÓSTICO
            // -----------------------------------------------------------------
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("                     DIAGNÓSTICO FINAL                     ");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("  El AStarPathFinder actual utiliza un pool de matrices");
            Console.WriteLine("  [ThreadStatic] con contadores de generación (O(1) reset).");
            Console.WriteLine("  Esto significa que NO HAY asignaciones dinámicas pesadas");
            Console.WriteLine("  de memoria (bool[,], double[,], GridPos[,]) por llamada.");
            Console.WriteLine();
            Console.WriteLine("  Las únicas asignaciones restantes provienen del BinaryHeap");
            Console.WriteLine("  y de la Lista<GridPos> devuelta como resultado.");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("  Benchmark finalizado. Presiona cualquier tecla para salir.");
            Console.ReadKey();
        }
    }
}
