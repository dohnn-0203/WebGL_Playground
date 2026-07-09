using System;
using MergeCafe.Board;
using MergeCafe.Data;

namespace MergeCafe.Generators
{
    public enum SpawnResultCode
    {
        Ok,
        NoEnergy,
        BoardFull
    }

    public readonly struct SpawnResult
    {
        public readonly SpawnResultCode Code;
        public readonly int CellIndex;
        public readonly ItemInstance Item;

        public SpawnResult(SpawnResultCode code, int cellIndex, ItemInstance item)
        {
            Code = code;
            CellIndex = cellIndex;
            Item = item;
        }

        public static SpawnResult Fail(SpawnResultCode code) => new SpawnResult(code, -1, null);
    }

    /// <summary>
    /// Owns the shared <see cref="EnergyPool"/> and the spawn rule: tapping a generator
    /// costs 1 shared energy and drops its Lv.1 item into the first free cell.
    /// </summary>
    public sealed class GeneratorManager
    {
        public EnergyPool Energy { get; }

        /// <summary>Raised when the shared energy changes.</summary>
        public event Action StatesChanged;

        public GeneratorManager(double nowUnix)
        {
            Energy = new EnergyPool(nowUnix);
            Energy.Changed += () => StatesChanged?.Invoke();
        }

        /// <summary>Places the three generator tiles on their starting cells.</summary>
        public void PlaceInitialGenerators(BoardManager board)
        {
            foreach (GeneratorDefinition def in GeneratorCatalog.All)
            {
                if (board.IsFreeCell(def.InitialCell))
                    board.TryPlaceGenerator(def.InitialCell, def.Output);
            }
        }

        /// <summary>Applies wall-clock energy recovery. Returns true if anything changed.</summary>
        public bool Tick(double nowUnix)
        {
            return Energy.Recover(nowUnix);
        }

        public SpawnResult TrySpawn(ItemType type, BoardManager board, double nowUnix)
        {
            if (!Energy.HasEnergy)
                return SpawnResult.Fail(SpawnResultCode.NoEnergy);
            if (!board.TryFindEmptyCell(out int cellIndex))
                return SpawnResult.Fail(SpawnResultCode.BoardFull);

            Energy.TrySpend(nowUnix);
            var item = new ItemInstance(type, 1);
            board.TryPlaceItem(cellIndex, item);

            StatesChanged?.Invoke();
            return new SpawnResult(SpawnResultCode.Ok, cellIndex, item);
        }

        public void RaiseStatesChanged() => StatesChanged?.Invoke();
    }
}
