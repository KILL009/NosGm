using System;

namespace Frostvein.GameObject.EventArguments
{
    public class CraftRecipeEventArgs : EventArgs
    {
        public CraftRecipeEventArgs(Item item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        public Item Item { get; }

        public int Amount { get; set; }
    }
}
