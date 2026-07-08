namespace MergeCafe.Data
{
    /// <summary>A single item sitting on the board.</summary>
    public sealed class ItemInstance
    {
        public ItemType Type { get; }
        public int Level { get; }

        public ItemDefinition Definition => ItemCatalog.Get(Type, Level);

        public ItemInstance(ItemType type, int level)
        {
            Type = type;
            Level = level;
        }

        public bool SameKindAs(ItemInstance other)
        {
            return other != null && other.Type == Type && other.Level == Level;
        }

        public override string ToString() => $"{Type} Lv.{Level}";
    }
}
