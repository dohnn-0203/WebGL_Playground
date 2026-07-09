using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Generators;
using NUnit.Framework;

namespace MergeCafe.Tests
{
    public sealed class EnergyPoolTests
    {
        private const double T0 = 1_000_000.0;

        [Test]
        public void StartsFull_AtBaseMax()
        {
            var pool = new EnergyPool(T0);
            Assert.AreEqual(EnergyPool.BaseMax, pool.Max);
            Assert.AreEqual(EnergyPool.BaseMax, pool.Current);
            Assert.IsTrue(pool.HasEnergy);
        }

        [Test]
        public void TrySpend_ReducesAndFailsAtZero()
        {
            var pool = new EnergyPool(T0) { Max = 3, Current = 2 };
            Assert.IsTrue(pool.TrySpend(T0));
            Assert.IsTrue(pool.TrySpend(T0));
            Assert.AreEqual(0, pool.Current);
            Assert.IsFalse(pool.TrySpend(T0));
        }

        [Test]
        public void Recover_AddsOnePerFiveSeconds_CapsAtMax()
        {
            var pool = new EnergyPool(T0) { Max = 20, Current = 5, LastRecoveryUnix = T0 };

            Assert.IsFalse(pool.Recover(T0 + 4));
            Assert.AreEqual(5, pool.Current);

            Assert.IsTrue(pool.Recover(T0 + 11)); // two intervals + 1s remainder
            Assert.AreEqual(7, pool.Current);
            Assert.AreEqual(4, pool.SecondsToNextRecovery(T0 + 11), 0.001);

            pool.Current = 19;
            pool.LastRecoveryUnix = T0;
            pool.Recover(T0 + 500);
            Assert.AreEqual(20, pool.Current);
            Assert.AreEqual(0, pool.SecondsToNextRecovery(T0 + 500), 0.001);
        }

        [Test]
        public void Recover_ClockBackwards_ResetsAnchorSafely()
        {
            var pool = new EnergyPool(T0) { Max = 20, Current = 5, LastRecoveryUnix = T0 };
            Assert.IsFalse(pool.Recover(T0 - 100));
            Assert.AreEqual(5, pool.Current);
            Assert.AreEqual(T0 - 100, pool.LastRecoveryUnix, 0.001);
        }
    }

    public sealed class GeneratorManagerTests
    {
        private const double T0 = 1_000_000.0;

        private BoardManager _board;
        private GeneratorManager _generators;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardManager();
            _generators = new GeneratorManager(T0);
        }

        [Test]
        public void SharedEnergy_StartsFull()
        {
            Assert.AreEqual(20, _generators.Energy.Max);
            Assert.AreEqual(20, _generators.Energy.Current);
        }

        [Test]
        public void PlaceInitialGenerators_PlacesAllThree()
        {
            _generators.PlaceInitialGenerators(_board);

            Assert.IsTrue(_board.HasGenerator(GeneratorCatalog.CoffeeMachine.InitialCell));
            Assert.IsTrue(_board.HasGenerator(GeneratorCatalog.Oven.InitialCell));
            Assert.IsTrue(_board.HasGenerator(GeneratorCatalog.Fridge.InitialCell));
            Assert.AreEqual(ItemType.Coffee, _board.GetGenerator(GeneratorCatalog.CoffeeMachine.InitialCell));
        }

        [Test]
        public void TrySpawn_PlacesLv1Item_AndSpendsSharedEnergy()
        {
            SpawnResult result = _generators.TrySpawn(ItemType.Coffee, _board, T0);

            Assert.AreEqual(SpawnResultCode.Ok, result.Code);
            Assert.AreEqual(ItemType.Coffee, result.Item.Type);
            Assert.AreEqual(1, result.Item.Level);
            Assert.AreSame(result.Item, _board.GetItem(result.CellIndex));
            Assert.AreEqual(19, _generators.Energy.Current);
        }

        [Test]
        public void TrySpawn_NoEnergy_Fails()
        {
            _generators.Energy.Current = 0;

            SpawnResult result = _generators.TrySpawn(ItemType.Coffee, _board, T0);

            Assert.AreEqual(SpawnResultCode.NoEnergy, result.Code);
            Assert.AreEqual(35, _board.FreeCellCount);
        }

        [Test]
        public void TrySpawn_FullBoard_Fails_WithoutSpendingEnergy()
        {
            for (int i = 0; i < BoardManager.CellCount; i++)
                if (_board.IsFreeCell(i))
                    _board.TryPlaceItem(i, new ItemInstance(ItemType.Coffee, 5));

            SpawnResult result = _generators.TrySpawn(ItemType.Coffee, _board, T0);

            Assert.AreEqual(SpawnResultCode.BoardFull, result.Code);
            Assert.AreEqual(20, _generators.Energy.Current);
        }

        [Test]
        public void Tick_RecoversSharedEnergy()
        {
            _generators.Energy.Current = 5;
            _generators.Energy.LastRecoveryUnix = T0;

            Assert.IsFalse(_generators.Tick(T0 + 4));
            Assert.AreEqual(5, _generators.Energy.Current);

            Assert.IsTrue(_generators.Tick(T0 + 5));
            Assert.AreEqual(6, _generators.Energy.Current);
        }
    }
}
