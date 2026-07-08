using MergeCafe.Data;
using NUnit.Framework;

namespace MergeCafe.Tests
{
    public sealed class ItemCatalogTests
    {
        [Test]
        public void Catalog_ContainsAll15Definitions()
        {
            Assert.AreEqual(15, ItemCatalog.AllDefinitions.Count);
        }

        [TestCase(ItemType.Coffee, new[] { 5, 15, 35, 80, 160 })]
        [TestCase(ItemType.Bread, new[] { 5, 18, 45, 100, 210 })]
        [TestCase(ItemType.Dessert, new[] { 8, 22, 55, 130, 260 })]
        public void SellPrices_MatchSpecTables(ItemType type, int[] expectedPrices)
        {
            for (int level = 1; level <= ItemCatalog.MaxLevel; level++)
                Assert.AreEqual(expectedPrices[level - 1], ItemCatalog.Get(type, level).SellPrice,
                    $"{type} Lv.{level} price");
        }

        [Test]
        public void DisplayNames_MatchSpecSpotChecks()
        {
            Assert.AreEqual("원두 커피", ItemCatalog.Get(ItemType.Coffee, 1).DisplayName);
            Assert.AreEqual("라떼", ItemCatalog.Get(ItemType.Coffee, 4).DisplayName);
            Assert.AreEqual("크루아상", ItemCatalog.Get(ItemType.Bread, 3).DisplayName);
            Assert.AreEqual("시그니처 케이크", ItemCatalog.Get(ItemType.Dessert, 5).DisplayName);
        }

        [Test]
        public void ShortLabels_UseFamilyPrefixAndLevel()
        {
            Assert.AreEqual("C1", ItemCatalog.Get(ItemType.Coffee, 1).ShortLabel);
            Assert.AreEqual("B2", ItemCatalog.Get(ItemType.Bread, 2).ShortLabel);
            Assert.AreEqual("D5", ItemCatalog.Get(ItemType.Dessert, 5).ShortLabel);
        }

        [Test]
        public void TryGet_InvalidLevels_ReturnFalse()
        {
            Assert.IsFalse(ItemCatalog.TryGet(ItemType.Coffee, 0, out _));
            Assert.IsFalse(ItemCatalog.TryGet(ItemType.Coffee, 6, out _));
            Assert.IsFalse(ItemCatalog.IsValid(ItemType.Dessert, -1));
        }
    }
}
