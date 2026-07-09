using System;
using System.Collections.Generic;
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
        /// <summary>Produced items drop into a free cell within this Chebyshev radius of the generator.</summary>
        public const int SpawnRadius = 2;

        public EnergyPool Energy { get; }

        private readonly Func<float> _rng01;

        /// <summary>Raised when the shared energy changes.</summary>
        public event Action StatesChanged;

        public GeneratorManager(double nowUnix, Func<float> rng01 = null)
        {
            Energy = new EnergyPool(nowUnix);
            Energy.Changed += () => StatesChanged?.Invoke();
            _rng01 = rng01 ?? (() => UnityEngine.Random.value);
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

        /// <summary>
        /// Produces the generator's item into a random free cell near <paramref name="originCell"/>
        /// (the generator's own cell). Falls back to the closest free cell if the neighbourhood is full.
        /// </summary>
        public SpawnResult TrySpawn(ItemType type, BoardManager board, int originCell, double nowUnix)
        {
            if (!Energy.HasEnergy)
                return SpawnResult.Fail(SpawnResultCode.NoEnergy);

            int cellIndex = PickSpawnCell(board, originCell);
            if (cellIndex < 0)
                return SpawnResult.Fail(SpawnResultCode.BoardFull);

            Energy.TrySpend(nowUnix);
            var item = new ItemInstance(type, 1);
            board.TryPlaceItem(cellIndex, item);

            StatesChanged?.Invoke();
            return new SpawnResult(SpawnResultCode.Ok, cellIndex, item);
        }

        /// <summary>Random free cell within SpawnRadius of the origin, else the globally nearest free cell.</summary>
        private int PickSpawnCell(BoardManager board, int originCell)
        {
            int originRow = BoardManager.RowOf(originCell);
            int originCol = BoardManager.ColOf(originCell);

            var near = new List<int>();
            int nearest = -1;
            int nearestDist = int.MaxValue;

            for (int i = 0; i < BoardManager.CellCount; i++)
            {
                if (!board.IsFreeCell(i))
                    continue;

                int dist = Math.Max(
                    Math.Abs(BoardManager.RowOf(i) - originRow),
                    Math.Abs(BoardManager.ColOf(i) - originCol));

                if (dist <= SpawnRadius)
                    near.Add(i);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = i;
                }
            }

            if (near.Count > 0)
            {
                int pick = Math.Min((int)(_rng01() * near.Count), near.Count - 1);
                return near[pick];
            }
            return nearest;
        }

        public void RaiseStatesChanged() => StatesChanged?.Invoke();
    }
}
