using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Generators;
using NUnit.Framework;

namespace MergeCafe.Tests
{
    public sealed class GeneratorManagerTests
    {
        private const double T0 = 1_000_000.0;

        private BoardManager _board;
        private GeneratorManager _generators;
        private float _nextRandom;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardManager();
            _nextRandom = 0.99f;
            _generators = new GeneratorManager(T0, () => _nextRandom);
        }

        [Test]
        public void InitialStates_MatchSpecTable()
        {
            GeneratorState coffee = _generators.Get(ItemType.Coffee);
            Assert.IsTrue(coffee.Unlocked);
            Assert.AreEqual(10, coffee.Energy);
            Assert.AreEqual(10, coffee.MaxEnergy);
            Assert.AreEqual(30, coffee.Definition.RecoverySeconds);

            GeneratorState oven = _generators.Get(ItemType.Bread);
            Assert.IsFalse(oven.Unlocked);
            Assert.AreEqual(8, oven.Energy);
            Assert.AreEqual(150, oven.Definition.UnlockCost);

            GeneratorState fridge = _generators.Get(ItemType.Dessert);
            Assert.IsFalse(fridge.Unlocked);
            Assert.AreEqual(6, fridge.Energy);
            Assert.AreEqual(45, fridge.Definition.RecoverySeconds);
            Assert.AreEqual(300, fridge.Definition.UnlockCost);
        }

        [Test]
        public void TrySpawn_PlacesLv1ItemAndConsumesEnergy()
        {
            SpawnResult result = _generators.TrySpawn(ItemType.Coffee, _board, T0);

            Assert.AreEqual(SpawnResultCode.Ok, result.Code);
            Assert.AreEqual(BoardManager.IndexOf(1, 1), result.CellIndex);
            Assert.AreEqual(ItemType.Coffee, result.Item.Type);
            Assert.AreEqual(1, result.Item.Level);
            Assert.AreSame(result.Item, _board.GetItem(result.CellIndex));
            Assert.AreEqual(9, _generators.Get(ItemType.Coffee).Energy);
        }

        [Test]
        public void TrySpawn_LockedGenerator_Fails()
        {
            SpawnResult result = _generators.TrySpawn(ItemType.Bread, _board, T0);

            Assert.AreEqual(SpawnResultCode.GeneratorLocked, result.Code);
            Assert.AreEqual(16, _board.EmptyUnlockedCount);
            Assert.AreEqual(8, _generators.Get(ItemType.Bread).Energy);
        }

        [Test]
        public void TrySpawn_WithoutEnergy_Fails()
        {
            GeneratorState coffee = _generators.Get(ItemType.Coffee);
            coffee.Energy = 0;

            SpawnResult result = _generators.TrySpawn(ItemType.Coffee, _board, T0);

            Assert.AreEqual(SpawnResultCode.NoEnergy, result.Code);
            Assert.AreEqual(16, _board.EmptyUnlockedCount);
        }

        [Test]
        public void TrySpawn_FullBoard_FailsWithoutConsumingEnergy()
        {
            for (int i = 0; i < BoardManager.CellCount; i++)
            {
                if (_board.IsUnlocked(i))
                    _board.TryPlaceItem(i, new ItemInstance(ItemType.Coffee, 5));
            }

            SpawnResult result = _generators.TrySpawn(ItemType.Coffee, _board, T0);

            Assert.AreEqual(SpawnResultCode.BoardFull, result.Code);
            Assert.AreEqual(10, _generators.Get(ItemType.Coffee).Energy);
        }

        [Test]
        public void Recovery_AddsOneEnergyPerInterval()
        {
            _generators.TrySpawn(ItemType.Coffee, _board, T0);
            GeneratorState coffee = _generators.Get(ItemType.Coffee);
            Assert.AreEqual(9, coffee.Energy);

            Assert.IsFalse(_generators.Tick(T0 + 29));
            Assert.AreEqual(9, coffee.Energy);

            Assert.IsTrue(_generators.Tick(T0 + 30));
            Assert.AreEqual(10, coffee.Energy);
        }

        [Test]
        public void Recovery_AccumulatesMultipleIntervals_AndKeepsRemainder()
        {
            GeneratorState coffee = _generators.Get(ItemType.Coffee);
            coffee.Energy = 5;
            coffee.LastRecoveryUnix = T0;

            _generators.Tick(T0 + 65); // two full 30s intervals + 5s remainder

            Assert.AreEqual(7, coffee.Energy);
            Assert.AreEqual(25, coffee.SecondsToNextRecovery(T0 + 65), 0.001);
        }

        [Test]
        public void Recovery_NeverExceedsMaxEnergy()
        {
            GeneratorState coffee = _generators.Get(ItemType.Coffee);
            coffee.Energy = 9;
            coffee.LastRecoveryUnix = T0;

            _generators.Tick(T0 + 300);

            Assert.AreEqual(10, coffee.Energy);
            Assert.AreEqual(0, coffee.SecondsToNextRecovery(T0 + 300), 0.001);
        }

        [Test]
        public void Recovery_ClockMovingBackwards_ResetsAnchorSafely()
        {
            GeneratorState coffee = _generators.Get(ItemType.Coffee);
            coffee.Energy = 5;
            coffee.LastRecoveryUnix = T0;

            _generators.Tick(T0 - 500);

            Assert.AreEqual(5, coffee.Energy);
            Assert.AreEqual(T0 - 500, coffee.LastRecoveryUnix, 0.001);
        }

        [Test]
        public void UpgradeLevel3_SpawnsLv2WithTenPercentChance()
        {
            GeneratorState coffee = _generators.Get(ItemType.Coffee);
            coffee.UpgradeLevel = 3;
            Assert.AreEqual(15, coffee.MaxEnergy); // base 10 + bonus 5

            _nextRandom = 0.05f; // below 0.10 → Lv.2
            SpawnResult lucky = _generators.TrySpawn(ItemType.Coffee, _board, T0);
            Assert.AreEqual(2, lucky.Item.Level);

            _nextRandom = 0.50f; // above 0.10 → Lv.1
            SpawnResult normal = _generators.TrySpawn(ItemType.Coffee, _board, T0);
            Assert.AreEqual(1, normal.Item.Level);
        }

        [Test]
        public void UpgradeTable_MatchesSpec()
        {
            Assert.AreEqual(0, GeneratorCatalog.EnergyBonus(1));
            Assert.AreEqual(3, GeneratorCatalog.EnergyBonus(2));
            Assert.AreEqual(5, GeneratorCatalog.EnergyBonus(3));
            Assert.AreEqual(8, GeneratorCatalog.EnergyBonus(4));

            Assert.AreEqual(0f, GeneratorCatalog.Level2Chance(2));
            Assert.AreEqual(0.10f, GeneratorCatalog.Level2Chance(3));
            Assert.AreEqual(0.20f, GeneratorCatalog.Level2Chance(4));

            Assert.AreEqual(200, GeneratorCatalog.UpgradeCost(2));
            Assert.AreEqual(500, GeneratorCatalog.UpgradeCost(3));
            Assert.AreEqual(1000, GeneratorCatalog.UpgradeCost(4));
        }
    }
}
