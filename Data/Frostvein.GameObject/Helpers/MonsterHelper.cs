using Frostvein.Domain;

namespace Frostvein.GameObject.Helpers
{
    public class MonsterHelper
    {
        #region Methods

        public static FactionType GetFaction(short monsterVNum)
        {
            switch (monsterVNum)
            {
                case 679:
                case 972:
                case 965:
                case 967:
                    return FactionType.Angel;

                case 680:
                case 973:
                case 966:
                case 968:
                    return FactionType.Demon;
            }

            return FactionType.None;
        }

        public static bool IsAutoTarget(short vnum)
        {
            switch (vnum)
            {
                case 105:
                case 106:
                case 107:
                case 108:
                case 109:
                case 110:
                case 111:
                case 112:
                case 113:
                case 181:
                case 200:
                case 201:
                case 202:
                case 203:
                case 555:
                case 681:
                case 728:
                case 1056:
                case 1420:
                case 1423:
                case 1501:
                case 1502:
                case 1921:
                case 2300:
                case 2301:
                case 2302:
                case 2303:
                case 2304:
                case 2314:
                case 2315:
                case 2331:
                case 2332:
                case 2333:
                case 2334:
                case 2354:
                case 2359:
                case 2363:
                case 2500:
                case 2501:
                case 2502:
                case 2505:
                case 2506:
                case 2507:
                case 2508:
                case 2509:
                case 2510:
                case 2511:
                case 2512:
                case 2516:
                case 2517:
                case 2518:
                case 2519:
                case 2521:
                case 2522:
                case 2523:
                case 2524:
                case 2525:
                case 2527:
                case 2530:
                case 2532:
                case 2533:
                case 2534:
                case 2535:
                case 2536:
                case 2537:
                case 2538:
                case 2539:
                case 2540:
                case 2543:
                case 2544:
                case 2545:
                case 2546:
                case 2547:
                case 2548:
                case 2549:
                case 2552:
                case 2553:
                case 2559:
                case 2560:
                case 2561:
                case 2562:
                case 2563:
                case 2564:
                case 2567:
                case 2568:
                case 2569:
                case 2570:
                case 2571:
                case 2572:
                case 2573:
                case 2575:
                case 2580:
                case 2590:
                case 2597:
                case 2602:
                case 2603:
                case 2639:
                case 2662:
                case 2663:
                case 2664:
                case 2665:
                case 2666:
                case 2667:
                case 2668:
                case 2678:
                case 3000:
                case 3001:
                case 3003:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsKamikaze(short monsterVNum)
        {
            switch (monsterVNum)
            {
                case 854: // Black Meteorite
                case 945: // Bomb
                case 974: // SP2AM Lotus Flower
                case 946: // Fire Mine
                case 1382: // Pumpkin Bomb
                case 1436: // Mobile Trap
                case 1439: // Giant Swirl
                case 2015: // Valakus' Hatchling
                case 2048: // ???
                case 2112: // Summoned Dark Clone (Fire)
                case 2113: // Summoned Dark Clone (Ice)
                case 2114: // Summoned Dark Clone (Light)
                case 2115: // Summoned Dark Clone (Dark)
                    return true;
            }

            return false;
        }

        public static bool IsNamaju(short monsterVNum)
        {
            switch (monsterVNum)
            {
                case 414: // Namaju
                case 428: // Angry Namaju
                case 429: // Angry Namaju
                case 430: // Angry Namaju
                case 431: // Angry Namaju
                case 432: // Angry Namaju
                    return true;
            }

            return false;
        }

        public static bool IsSelfAttack(short monsterVNum)
        {
            switch (monsterVNum)
            {
                case 2328: // No name (Laurena's Thunderbolt)
                    return true;
            }

            return false;
        }

        public static bool UseOwnerEntity(short monsterVNum)
        {
            switch (monsterVNum)
            {
                case 416: // Mini Jajamaru
                case 945: // SP3A Bomb
                case 946: // SP3A Fire Mine
                case 974: // SP2AM Lotus Flower
                case 2112: // Summoned Dark Clone (Fire)
                case 2113: // Summoned Dark Clone (Ice)
                case 2114: // Summoned Dark Clone (Light)
                case 2115: // Summoned Dark Clone (Dark)
                    return true;
            }

            return false;
        }

        #endregion
    }
}