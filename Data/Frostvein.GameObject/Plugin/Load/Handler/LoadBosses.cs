using Frostvein.GameObject.Networking;
using System.Collections.Generic;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadBosses
    {
        public static void Load()
        {
            ServerManager.Instance.BossVNums = new List<short>
            {
                580, //Perro infernal
                582, //Ginseng fuerte
                583, //Basilisco fuerte
                584, //Rey Tubérculo
                585, //Gran Rey Tubérculo
                586, //Rey Tubérculo monstruoso
                588, //Pollito gigante
                589, //Mandra milenaria
                590, //Perro guardián del infierno
                591, //Gallo de pelea gigante
                592, //Patata milenaria
                593, //Perro de Satanás
                594, //Barepardo gigante
                595, //Boing satélite
                596, //Raíz demoníaca
                597, //Slade musculoso
                598, //Rey de las tinieblas
                599, //Gólem slade
                600, //Mini Castra
                601, //Caballero de la muerte
                602, //Sanguijuela gorda
                603, //Ginseng milenario
                604, //Rey basilisco
                605, //Gigante guerrero
                606, //Árbol maldito
                607, //Rey cangrejo
                2529, //Castra Oscuro
                1904, //Sombra de Kertos
                796, //Rey Pollo
                388, //Rey Pollo
                774, //Reina Gallina
                2331, //Diablilla Hongbi
                2332, //Diablilla Cheongbi
                2309, //Vulpina
                2322, //Maru
                1381, //Jack O´Lantern
                2357, //Lola Cucharón
                533, //Cabeza grande de muñeco de nieve
                1500, //Capitán Pete O'Peng
                282, //Mamá cubi
                284, //Ginseng
                285, //Castra Oscuro
                289, //Araña negra gigante
                286, //Slade gigante
                587, //Lord Mukraju
                563, //Maestro Morcos
                629, //Lady Calvina
                624, //Lord Berios
                577, //Maestro Hatus
                557, //Ross Hisrant
                2305, //Caligor
                2326, //Bruja Laurena
                2327, //Bestia de Laurena
                2639, //Yertirán podrido
                1028, //Ibrahim
                2034, //Lord Draco
                2049, //Glacerus, el frío
                1044, //Valakus, Rey del Fuego
                1905, //Sombra de Valakus
                1046, //Kertos, el Perro Demonio
                1912, //Sombra de Kertos
                637, //Perro demonio Kertos fuerte
                1099, //Fantasma de Grenigas
                1058, //Grenigas, el Dios del Fuego
                2619, //Fafnir, el Codicioso
                2504, //Zenas
                2514, //Erenia
                2574, //Fernon incompleta
                679, //Guardián de los ángeles
                680, //Guardián de los demonios
                967, //Altar de los ángeles
                968, //Altar de los diablo
                533 // Huge Snowman Head
            };
            ServerManager.Instance.MapBossVNums = new List<short>
            {
                580, //Perro infernal
                582, //Ginseng fuerte
                583, //Basilisco fuerte
                584, //Rey Tubérculo
                585, //Gran Rey Tubérculo
                586, //Rey Tubérculo monstruoso
                588, //Pollito gigante
                589, //Mandra milenaria
                590, //Perro guardián del infierno
                591, //Gallo de pelea gigante
                592, //Patata milenaria
                593, //Perro de Satanás
                594, //Barepardo gigante
                595, //Boing satélite
                596, //Raíz demoníaca
                597, //Slade musculoso
                598, //Rey de las tinieblas
                599, //Gólem slade
                600, //Mini Castra
                601, //Caballero de la muerte
                602, //Sanguijuela gorda
                603, //Ginseng milenario
                604, //Rey basilisco
                605, //Gigante guerrero
                606, //Árbol maldito
                607, //Rey cangrejo
                2529, //Castra Oscuro
                1904, //Sombra de Kertos
                1346, //Baúl de la banda de ladrones
                1348, //Baúl extraño
                1384, //Halloween
                1906,
                1905,
                2350
            };

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance.BossVNums.Count} Bosses - Status: Successful", Domain.LogType.LOAD);
            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance.MapBossVNums.Count} Map Bosses - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
