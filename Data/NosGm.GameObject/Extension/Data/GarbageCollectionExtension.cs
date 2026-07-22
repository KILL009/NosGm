using NosGm.GameObject.Extension.Message;
using System;

namespace NosGm.GameObject.Extension
{
    public static class GarbageCollectionExtension
    {
        public static void Run()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public static void RunWithResponse(ClientSession Session)
        {
            long totalMemory = GC.GetTotalMemory(true);
            long startMemory = GC.GetTotalMemory(true);

            GC.Collect();

            long endMemory = GC.GetTotalMemory(true);
            long releasedMemory = startMemory - endMemory;
            MessageExtension.SendYellow(Session, $"[Titan Shield]\nTotal Memory: {totalMemory}\nReleased Memory: {endMemory}");
        }
    }
}