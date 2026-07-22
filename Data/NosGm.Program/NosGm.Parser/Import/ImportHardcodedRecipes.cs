using NosGm.DAL;
using NosGm.Data;
using System.Linq;

namespace NosGm.Parser.Import
{
    public class ImportHardcodedRecipes : IImport
    {
        public void Import()
        {
            // Production Tools for Adventurers
            InsertRecipe(6, 1035, 1, new short[] { 1027, 10, 2038, 8, 1035, 1 });
            InsertRecipe(16, 1035, 1, new short[] { 1027, 8, 2042, 6, 1035, 1 });
            InsertRecipe(204, 1035, 1, new short[] { 1027, 15, 2042, 10, 1035, 1 });
            InsertRecipe(206, 1035, 1, new short[] { 1027, 8, 2046, 7, 1035, 1 });
            InsertRecipe(501, 1035, 1, new short[] { 1027, 14, 500, 1, 1035, 1 });

            // Production Tools for Swordsmen
            InsertRecipe(22, 1039, 1, new short[] { 1027, 30, 2035, 32, 1039, 1 });
            InsertRecipe(26, 1039, 1, new short[] { 1027, 43, 2035, 44, 1039, 1 });
            InsertRecipe(30, 1039, 1, new short[] { 1027, 54, 2036, 56, 1039, 1 });
            InsertRecipe(73, 1039, 1, new short[] { 1027, 33, 2035, 10, 2039, 23, 1039, 1 });
            InsertRecipe(76, 1039, 1, new short[] { 1027, 53, 2036, 14, 2040, 39, 1039, 1 });
            InsertRecipe(96, 1039, 1, new short[] { 1027, 20, 2034, 6, 2046, 14, 1039, 1 });
            InsertRecipe(100, 1039, 1, new short[] { 1027, 35, 2035, 12, 2047, 23, 1039, 1 });
            InsertRecipe(104, 1039, 1, new short[] { 1027, 53, 2036, 18, 2048, 35, 1039, 1 });

            // Production Tools for Archers
            InsertRecipe(36, 1040, 1, new short[] { 1027, 30, 2039, 32, 1040, 1 });
            InsertRecipe(40, 1040, 1, new short[] { 1027, 43, 2039, 35, 1040, 1 });
            InsertRecipe(44, 1040, 1, new short[] { 1027, 54, 2040, 56, 1040, 1 });
            InsertRecipe(81, 1040, 1, new short[] { 1027, 33, 2035, 32, 1040, 1 });
            InsertRecipe(84, 1040, 1, new short[] { 1027, 53, 2036, 54, 1040, 1 });
            InsertRecipe(109, 1040, 1, new short[] { 1027, 20, 2042, 8, 2046, 12, 1040, 1 });
            InsertRecipe(113, 1040, 1, new short[] { 1027, 35, 2043, 13, 2047, 22, 1040, 1 });
            InsertRecipe(117, 1040, 1, new short[] { 1027, 53, 2044, 20, 2048, 33, 1040, 1 });

            // Production Tools for Sorcerers
            InsertRecipe(50, 1041, 1, new short[] { 1027, 30, 2039, 32, 1041, 1 });
            InsertRecipe(54, 1041, 1, new short[] { 1027, 43, 2039, 45, 1041, 1 });
            InsertRecipe(58, 1041, 1, new short[] { 1027, 54, 2040, 56, 1041, 1 });
            InsertRecipe(89, 1041, 1, new short[] { 1027, 33, 2035, 34, 1041, 1 });
            InsertRecipe(92, 1041, 1, new short[] { 1027, 53, 2036, 54, 1041, 1 });
            InsertRecipe(122, 1041, 1, new short[] { 1027, 20, 2042, 14, 2046, 6, 1041, 1 });
            InsertRecipe(126, 1041, 1, new short[] { 1027, 35, 2043, 28, 2047, 7, 1041, 1 });
            InsertRecipe(130, 1041, 1, new short[] { 1027, 53, 2044, 42, 2048, 11, 1041, 1 });

            // Production Tools for Accessories
            InsertRecipe(508, 1047, 1, new short[] { 1027, 24, 1032, 5, 1047, 1 });
            InsertRecipe(509, 1047, 1, new short[] { 1027, 25, 1031, 5, 1047, 1 });
            InsertRecipe(510, 1047, 1, new short[] { 1027, 26, 1031, 7, 1047, 1 });
            InsertRecipe(514, 1047, 1, new short[] { 1027, 33, 1033, 10, 1047, 1 });
            InsertRecipe(516, 1047, 1, new short[] { 1027, 35, 1032, 12, 1047, 1 });
            InsertRecipe(517, 1047, 1, new short[] { 1027, 36, 1034, 15, 1047, 1 });
            InsertRecipe(522, 1047, 1, new short[] { 1027, 43, 1033, 20, 1047, 1 });
            InsertRecipe(523, 1047, 1, new short[] { 1027, 44, 1031, 24, 1047, 1 });
            InsertRecipe(525, 1047, 1, new short[] { 1027, 46, 1034, 28, 1047, 1 });
            InsertRecipe(531, 1047, 1, new short[] { 1027, 54, 1032, 36, 1047, 1 });
            InsertRecipe(534, 1047, 1, new short[] { 1027, 57, 1033, 42, 1047, 1 });

            // Production Tools for Gems, Cellons and Crystals
            InsertRecipe(1016, 1072, 1, new short[] { 1014, 99, 1015, 5, 1072, 1 });
            InsertRecipe(1018, 1072, 1, new short[] { 1014, 5, 1017, 5, 1072, 1 });
            InsertRecipe(1019, 1072, 1, new short[] { 1014, 10, 1018, 5, 1072, 1 });
            InsertRecipe(1020, 1072, 1, new short[] { 1014, 17, 1019, 5, 1072, 1 });
            InsertRecipe(1021, 1072, 1, new short[] { 1014, 25, 1020, 5, 1072, 1 });
            InsertRecipe(1022, 1072, 1, new short[] { 1014, 35, 1021, 5, 1072, 1 });
            InsertRecipe(1023, 1072, 1, new short[] { 1014, 50, 1022, 5, 1072, 1 });
            InsertRecipe(1024, 1072, 1, new short[] { 1014, 75, 1023, 5, 1072, 1 });
            InsertRecipe(1025, 1072, 1, new short[] { 1014, 110, 1024, 5, 1072, 1 });
            InsertRecipe(1026, 1072, 1, new short[] { 1014, 160, 1025, 5, 1072, 1 });
            InsertRecipe(1029, 1072, 1, new short[] { 1014, 20, 1028, 5, 1072, 1 });
            InsertRecipe(1030, 1072, 1, new short[] { 1014, 50, 1029, 5, 1072, 1 });
            InsertRecipe(1031, 1072, 4, new short[] { 1028, 1, 2097, 5, 1072, 1 });
            InsertRecipe(1032, 1072, 4, new short[] { 1028, 1, 2097, 5, 1072, 1 });
            InsertRecipe(1033, 1072, 4, new short[] { 1028, 1, 2097, 5, 1072, 1 });
            InsertRecipe(1034, 1072, 4, new short[] { 1028, 1, 2097, 5, 1072, 1 });

            // Production Tools for Raw Materials
            InsertRecipe(2035, 1073, 1, new short[] { 1014, 5, 2034, 5, 1073, 1 });
            InsertRecipe(2036, 1073, 1, new short[] { 1014, 10, 2035, 5, 1073, 1 });
            InsertRecipe(2037, 1073, 1, new short[] { 1014, 20, 2036, 5, 1073, 1 });
            InsertRecipe(2039, 1073, 1, new short[] { 1014, 5, 2038, 5, 1073, 1 });
            InsertRecipe(2040, 1073, 1, new short[] { 1014, 10, 2039, 5, 1073, 1 });
            InsertRecipe(2041, 1073, 1, new short[] { 1014, 20, 2040, 5, 1073, 1 });
            InsertRecipe(2043, 1073, 1, new short[] { 1014, 5, 2042, 5, 1073, 1 });
            InsertRecipe(2044, 1073, 1, new short[] { 1014, 10, 2043, 5, 1073, 1 });
            InsertRecipe(2045, 1073, 1, new short[] { 1014, 20, 2044, 5, 1073, 1 });
            InsertRecipe(2047, 1073, 1, new short[] { 1014, 5, 2046, 5, 1073, 1 });
            InsertRecipe(2048, 1073, 1, new short[] { 1014, 10, 2047, 5, 1073, 1 });
            InsertRecipe(2049, 1073, 1, new short[] { 1014, 20, 2048, 5, 1073, 1 });

            // Production Tools for Gloves and Shoes
            InsertRecipe(718, 1083, 1, new short[] { 1027, 5, 1028, 1, 2042, 4, 1083, 1 });
            InsertRecipe(703, 1083, 1, new short[] { 1027, 7, 1028, 2, 2034, 5, 1083, 1 });
            InsertRecipe(705, 1083, 1, new short[] { 1027, 9, 1028, 3, 2035, 3, 1083, 1 });
            InsertRecipe(719, 1083, 1, new short[] { 1027, 12, 1028, 4, 2047, 5, 1083, 1 });
            InsertRecipe(722, 1083, 1, new short[] { 1027, 5, 1028, 1, 2046, 5, 1083, 1 });
            InsertRecipe(723, 1083, 1, new short[] { 1027, 7, 1028, 2, 2046, 7, 1083, 1 });
            InsertRecipe(724, 1083, 1, new short[] { 1027, 9, 1028, 3, 2047, 4, 1083, 1 });
            InsertRecipe(725, 1083, 1, new short[] { 1027, 14, 1028, 4, 2047, 7, 1083, 1 });
            InsertRecipe(325, 1083, 1, new short[] { 2044, 10, 2048, 10, 2093, 50, 1083, 1 });

            // Construction Plan (Level 1)
            InsertRecipe(3121, 1235, 1,
                new short[] { 2036, 50, 2037, 30, 2040, 20, 2105, 10, 2189, 20, 2205, 20, 1, 1235 });
            InsertRecipe(3122, 1235, 1,
                new short[] { 2040, 50, 2041, 30, 2048, 20, 2109, 10, 2190, 20, 2206, 20, 1, 1235 });
            InsertRecipe(3123, 1235, 1,
                new short[] { 2044, 20, 2048, 50, 2049, 30, 2117, 10, 2191, 20, 2207, 20, 1, 1235 });
            InsertRecipe(3124, 1235, 1,
                new short[] { 2036, 20, 2044, 50, 2045, 30, 2118, 10, 2192, 20, 2208, 20, 1, 1235 });

            // Construction Plan (Level 2)
            InsertRecipe(3125, 1236, 1,
                new short[] { 2037, 70, 2041, 40, 2048, 20, 2105, 20, 2189, 30, 2193, 30, 2197, 20, 2205, 40, 1236, 1 });
            InsertRecipe(3126, 1236, 1,
                new short[] { 2041, 70, 2044, 20, 2049, 40, 2109, 20, 2190, 30, 2194, 30, 2198, 20, 2206, 40, 1236, 1 });
            InsertRecipe(3127, 1236, 1,
                new short[] { 2036, 20, 2045, 40, 2049, 70, 2117, 20, 2191, 30, 2195, 30, 2199, 20, 2207, 40, 1236, 1 });
            InsertRecipe(3128, 1236, 1,
                new short[] { 2037, 40, 2040, 20, 2045, 70, 2118, 20, 2192, 30, 2196, 30, 2200, 20, 2208, 40, 1236, 1 });

            // Boot Combination Recipe A
            InsertRecipe(384, 1237, 1, new short[] { 1027, 30, 1032, 10, 2010, 10, 2044, 30, 2208, 10, 1237, 1 });
            InsertRecipe(385, 1237, 1, new short[] { 1027, 30, 1031, 10, 2010, 10, 2036, 30, 2205, 10, 1237, 1 });
            InsertRecipe(386, 1237, 1, new short[] { 1027, 30, 1033, 10, 2010, 10, 2040, 30, 2206, 10, 1237, 1 });
            InsertRecipe(387, 1237, 1, new short[] { 1027, 30, 1034, 10, 2010, 10, 2048, 30, 2207, 10, 1237, 1 });

            // Boot Combination Recipe B
            InsertRecipe(388, 1238, 1, new short[] { 1027, 50, 1030, 5, 2010, 20, 2204, 10, 2210, 5, 1238, 1 });
            InsertRecipe(389, 1238, 1, new short[] { 1027, 50, 1030, 5, 2010, 20, 2201, 10, 2209, 5, 1238, 1 });
            InsertRecipe(390, 1238, 1, new short[] { 1027, 50, 1030, 5, 2010, 20, 2202, 10, 2211, 5, 1238, 1 });
            InsertRecipe(391, 1238, 1, new short[] { 1027, 50, 1030, 5, 2010, 20, 2203, 10, 2212, 5, 1238, 1 });

            // Glove Combination Recipe A
            InsertRecipe(376, 1239, 1, new short[] { 1027, 30, 1032, 10, 2010, 10, 2044, 30, 2208, 10, 1239, 1 });
            InsertRecipe(377, 1239, 1, new short[] { 1027, 30, 1031, 10, 2010, 10, 2036, 30, 2205, 10, 1239, 1 });
            InsertRecipe(378, 1239, 1, new short[] { 1027, 30, 1033, 10, 2010, 10, 2040, 30, 2206, 10, 1239, 1 });
            InsertRecipe(379, 1239, 1, new short[] { 1027, 30, 1034, 10, 2010, 10, 2048, 30, 2207, 10, 1239, 1 });

            // Glove Combination Recipe B
            InsertRecipe(380, 1240, 1, new short[] { 1027, 50, 1030, 5, 2010, 20, 2204, 10, 2210, 5, 1240, 1 });
            InsertRecipe(381, 1240, 1, new short[] { 1027, 50, 1030, 5, 2010, 20, 2201, 10, 2209, 5, 1240, 1 });
            InsertRecipe(382, 1240, 1, new short[] { 1027, 50, 1030, 5, 2010, 20, 2202, 10, 2211, 5, 1240, 1 });
            InsertRecipe(383, 1240, 1, new short[] { 1027, 50, 1030, 5, 2010, 20, 2203, 10, 2212, 5, 1240, 1 });

            // Consumables Recipe
            InsertRecipe(1245, 1241, 1, new short[] { 2029, 5, 2097, 5, 2196, 5, 2208, 5, 2215, 1, 1241, 1 });
            InsertRecipe(1246, 1241, 1, new short[] { 2029, 5, 2097, 5, 2193, 5, 2206, 5, 1241, 1 });
            InsertRecipe(1247, 1241, 1, new short[] { 2029, 5, 2097, 5, 2194, 5, 2207, 5, 1241, 1 });
            InsertRecipe(1248, 1241, 1, new short[] { 2029, 5, 2097, 5, 2195, 5, 2205, 5, 1241, 1 });
            InsertRecipe(1249, 1241, 1, new short[] { 2029, 5, 2097, 5, 2195, 5, 2205, 5, 1241, 1 });

            // Amir's Armour Parchment
            InsertRecipe(409, 1312, 1, new short[] { 298, 1, 2049, 70, 2227, 80, 2254, 5, 2265, 80, 1312, 1 });
            InsertRecipe(410, 1312, 1, new short[] { 296, 1, 2037, 70, 2246, 80, 2255, 5, 2271, 80, 1312, 1 });
            InsertRecipe(411, 1312, 1, new short[] { 272, 1, 2041, 70, 2252, 5, 2253, 80, 2270, 80, 1312, 1 });

            // Amir's Weapon Parchment A
            InsertRecipe(400, 1313, 1, new short[] { 263, 1, 2036, 60, 2218, 40, 2250, 10, 1313, 1 });
            InsertRecipe(402, 1313, 1, new short[] { 292, 1, 2040, 60, 2217, 50, 2249, 5, 2263, 30, 2279, 3, 1313, 1 });
            InsertRecipe(403, 1313, 1, new short[] { 266, 1, 2040, 60, 2217, 40, 2249, 10, 1313, 1 });
            InsertRecipe(405, 1313, 1, new short[] { 290, 1, 2044, 60, 2224, 50, 2251, 5, 2262, 3, 2275, 30, 1313, 1 });
            InsertRecipe(406, 1313, 1, new short[] { 269, 1, 2048, 60, 2224, 40, 2251, 10, 1313, 1 });
            InsertRecipe(408, 1313, 1, new short[] { 264, 1, 2036, 60, 2218, 50, 2222, 3, 2250, 5, 2276, 30, 1313, 1 });

            // Amir's Weapon Parchment B
            InsertRecipe(401, 1314, 1, new short[] { 400, 1, 2037, 99, 2222, 3, 2231, 70, 2257, 99, 1314, 1 });
            InsertRecipe(404, 1314, 1, new short[] { 403, 1, 2041, 99, 2219, 3, 2226, 70, 2277, 99, 1314, 1 });
            InsertRecipe(407, 1314, 1, new short[] { 406, 1, 2049, 99, 2245, 3, 2261, 70, 2269, 99, 1314, 1 });

            // Amir's Weapon Specification Book Cover
            InsertRecipe(1315, 1316, 1, new short[] { 1312, 10, 1313, 10, 1314, 10, 1316, 1 });

            // Ancelloan's Accessory Production Scroll
            InsertRecipe(4942, 5884, 1, new short[] { 4940, 1, 2805, 15, 2816, 5, 5881, 5, 2811, 30, 5884, 1 });
            InsertRecipe(4943, 5884, 1, new short[] { 4938, 1, 2805, 10, 2816, 3, 5881, 3, 2811, 20, 5884, 1 });
            InsertRecipe(4944, 5884, 1, new short[] { 4936, 1, 2805, 12, 2816, 4, 5881, 4, 2811, 25, 5884, 1 });
            InsertRecipe(4946, 5884, 1, new short[] { 4940, 1, 2805, 15, 2816, 5, 5880, 5, 2811, 30, 5884, 1 });
            InsertRecipe(4947, 5884, 1, new short[] { 4938, 1, 2805, 10, 2816, 3, 5880, 3, 2811, 20, 5884, 1 });
            InsertRecipe(4948, 5884, 1, new short[] { 4936, 1, 2805, 12, 2816, 4, 5880, 4, 2811, 25, 5884, 1 });

            // Ancelloan's Weapon Production Scroll
            InsertRecipe(4958, 5885, 1, new short[] { 4901, 1, 2805, 80, 2816, 60, 5880, 70, 2812, 35, 5885, 1 });
            InsertRecipe(4959, 5885, 1, new short[] { 4907, 1, 2805, 80, 2816, 60, 5880, 70, 2812, 35, 5885, 1 });
            InsertRecipe(4960, 5885, 1, new short[] { 4904, 1, 2805, 80, 2816, 60, 5880, 70, 2812, 35, 5885, 1 });
            InsertRecipe(4964, 5885, 1, new short[] { 4901, 1, 2805, 80, 2816, 60, 5881, 70, 2812, 35, 5885, 1 });
            InsertRecipe(4965, 5885, 1, new short[] { 4907, 1, 2805, 80, 2816, 60, 5881, 70, 2812, 35, 5885, 1 });
            InsertRecipe(4966, 5885, 1, new short[] { 4904, 1, 2805, 80, 2816, 60, 5881, 70, 2812, 35, 5885, 1 });

            // Ancelloan's Secondary Weapon Production Scroll
            InsertRecipe(4955, 5886, 1, new short[] { 4913, 1, 2805, 80, 2816, 60, 5880, 70, 2812, 35, 5886, 1 });
            InsertRecipe(4956, 5886, 1, new short[] { 4910, 1, 2805, 80, 2816, 60, 5880, 70, 2812, 35, 5886, 1 });
            InsertRecipe(4957, 5886, 1, new short[] { 4916, 1, 2805, 80, 2816, 60, 5880, 70, 2812, 35, 5886, 1 });
            InsertRecipe(4961, 5886, 1, new short[] { 4913, 1, 2805, 80, 2816, 60, 5881, 70, 2812, 35, 5886, 1 });
            InsertRecipe(4962, 5886, 1, new short[] { 4910, 1, 2805, 80, 2816, 60, 5881, 70, 2812, 35, 5886, 1 });
            InsertRecipe(4963, 5886, 1, new short[] { 4916, 1, 2805, 80, 2816, 60, 5881, 70, 2812, 35, 5886, 1 });

            // Ancelloan's Armour Production Scroll
            InsertRecipe(4949, 5887, 1,
                new short[] { 4919, 1, 2805, 80, 2816, 40, 5880, 10, 2818, 20, 2819, 10, 2811, 70, 5887, 1 });
            InsertRecipe(4950, 5887, 1,
                new short[] { 4925, 1, 2805, 60, 2816, 15, 5880, 10, 2814, 70, 2818, 10, 2819, 20, 5887, 1 });
            InsertRecipe(4951, 5887, 1,
                new short[] { 4922, 1, 2805, 70, 2816, 30, 5880, 70, 2814, 35, 2818, 15, 2819, 15, 2811, 35, 5887, 1 });
            InsertRecipe(4952, 5887, 1,
                new short[] { 4919, 1, 2805, 80, 2816, 40, 5881, 10, 2818, 20, 2819, 10, 2811, 90, 5887, 1 });
            InsertRecipe(4953, 5887, 1,
                new short[] { 4925, 1, 2805, 60, 2816, 15, 5881, 10, 2814, 70, 2818, 10, 2819, 20, 5887, 1 });
            InsertRecipe(4954, 5887, 1,
                new short[] { 4922, 1, 2805, 70, 2816, 30, 5881, 70, 2814, 35, 2818, 15, 2819, 15, 2811, 35, 5887, 1 });

            // Charred Mask Parchment
            InsertRecipe(4927, 5900, 1, new short[] { 2505, 3, 2506, 2, 2353, 30, 2355, 20, 5900, 1 });
            InsertRecipe(4928, 5900, 1, new short[] { 2505, 10, 2506, 8, 2507, 1, 2353, 90, 2356, 60, 5900, 3 });

            // Grenigas Accessories Parchment
            InsertRecipe(4936, 5901, 1, new short[] { 4935, 1, 2505, 4, 2506, 4, 2359, 20, 2360, 20, 2509, 5, 5901, 1 });
            InsertRecipe(4938, 5901, 1, new short[] { 4937, 1, 2505, 6, 2506, 2, 2359, 20, 2360, 20, 2510, 5, 5901, 1 });
            InsertRecipe(4940, 5901, 1, new short[] { 4939, 1, 2505, 2, 2506, 6, 2359, 20, 2360, 20, 2508, 5, 5901, 1 });

            // this implementation takes a FUCKTON of hardcoding, for fucks sake ENTWELL why u suck
            // soo much -_-
        }

        private void InsertRecipe(short itemVNum, short triggerVNum, short amount = 1, short[] recipeItems = null)
        {
            void recipeAdd(RecipeDTO recipeDTO)
            {
                var recipeList = DAOFactory.RecipeListDAO.LoadByRecipeId(recipeDTO.RecipeId)
                    .FirstOrDefault(r => r.ItemVNum == null);
                if (recipeList != null)
                {
                    recipeList.ItemVNum = triggerVNum;
                    DAOFactory.RecipeListDAO.Update(recipeList);
                }
                else
                {
                    recipeList = new RecipeListDTO
                    {
                        ItemVNum = triggerVNum,
                        RecipeId = recipeDTO.RecipeId
                    };
                    DAOFactory.RecipeListDAO.Insert(recipeList);
                }
            }

            var recipe = DAOFactory.RecipeDAO.LoadByItemVNum(itemVNum);
            if (recipe != null)
            {
                recipeAdd(recipe);
            }
            else
            {
                recipe = new RecipeDTO
                {
                    ItemVNum = itemVNum,
                    Amount = amount
                };
                DAOFactory.RecipeDAO.Insert(recipe);
                recipe = DAOFactory.RecipeDAO.LoadByItemVNum(itemVNum);
                if (recipe != null && recipeItems != null)
                {
                    for (var i = 0; i < recipeItems.Length; i += 2)
                    {
                        var recipeItem = new RecipeItemDTO
                        {
                            ItemVNum = recipeItems[i],
                            Amount = recipeItems[i + 1],
                            RecipeId = recipe.RecipeId
                        };
                        if (DAOFactory.RecipeItemDAO.LoadByRecipeAndItem(recipe.RecipeId, recipeItem.ItemVNum) == null)
                            DAOFactory.RecipeItemDAO.Insert(recipeItem);
                    }

                    recipeAdd(recipe);
                }
            }
        }
    }
}