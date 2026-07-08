using MergeCafe.Data;

namespace MergeCafe.Orders
{
    /// <summary>One customer order (webGL_game.md §12).</summary>
    public sealed class CafeOrder
    {
        public string orderId;
        public ItemType requiredItemType;
        public int requiredItemLevel;
        public int rewardGold;

        public CafeOrder(string orderId, ItemType requiredItemType, int requiredItemLevel, int rewardGold)
        {
            this.orderId = orderId;
            this.requiredItemType = requiredItemType;
            this.requiredItemLevel = requiredItemLevel;
            this.rewardGold = rewardGold;
        }
    }
}
