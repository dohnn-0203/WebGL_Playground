using System;

namespace MergeCafe.Economy
{
    /// <summary>Gold wallet (webGL_game.md §13). Starts at 0.</summary>
    public sealed class EconomyManager
    {
        public long Gold { get; private set; }

        public event Action<long> GoldChanged;

        public void AddGold(long amount)
        {
            if (amount <= 0)
                return;
            Gold += amount;
            GoldChanged?.Invoke(Gold);
        }

        public bool CanAfford(long cost) => Gold >= cost;

        public bool TrySpend(long cost)
        {
            if (cost < 0 || Gold < cost)
                return false;
            Gold -= cost;
            GoldChanged?.Invoke(Gold);
            return true;
        }

        /// <summary>Direct assignment used only when restoring a save.</summary>
        public void SetGold(long value)
        {
            Gold = Math.Max(0, value);
            GoldChanged?.Invoke(Gold);
        }
    }
}
