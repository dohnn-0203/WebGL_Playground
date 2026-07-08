using System;
using System.Collections.Generic;
using MergeCafe.Board;
using MergeCafe.Data;

namespace MergeCafe.Generators
{
    public enum SpawnResultCode
    {
        Ok,
        GeneratorLocked,
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
    /// Owns the three generator states and the spawn rules of webGL_game.md §11.
    /// The random source is injected so upgrade-chance logic stays testable.
    /// </summary>
    public sealed class GeneratorManager
    {
        private readonly Dictionary<ItemType, GeneratorState> _states =
            new Dictionary<ItemType, GeneratorState>();

        private readonly Func<float> _rng01;

        /// <summary>Raised whenever energy / unlock / upgrade state changes.</summary>
        public event Action StatesChanged;

        public GeneratorManager(double nowUnix, Func<float> rng01 = null)
        {
            _rng01 = rng01 ?? (() => UnityEngine.Random.value);
            foreach (GeneratorDefinition def in GeneratorCatalog.All)
                _states[def.Output] = new GeneratorState(def, nowUnix);
        }

        public GeneratorState Get(ItemType type) => _states[type];

        public IEnumerable<GeneratorState> All => _states.Values;

        /// <summary>Applies wall-clock recovery to all generators. Returns true if any energy changed.</summary>
        public bool Tick(double nowUnix)
        {
            bool changed = false;
            foreach (GeneratorState state in _states.Values)
            {
                int before = state.Energy;
                state.Recover(nowUnix);
                if (state.Energy != before)
                    changed = true;
            }

            if (changed)
                StatesChanged?.Invoke();
            return changed;
        }

        /// <summary>
        /// Spawn rules (§11): needs an unlocked generator, 1+ energy and an empty
        /// unlocked cell. Upgrade levels 3-4 may produce a Lv.2 item instead of Lv.1.
        /// </summary>
        public SpawnResult TrySpawn(ItemType type, BoardManager board, double nowUnix)
        {
            GeneratorState state = Get(type);

            if (!state.Unlocked)
                return SpawnResult.Fail(SpawnResultCode.GeneratorLocked);
            if (state.Energy <= 0)
                return SpawnResult.Fail(SpawnResultCode.NoEnergy);
            if (!board.TryFindEmptyCell(out int cellIndex))
                return SpawnResult.Fail(SpawnResultCode.BoardFull);

            int level = _rng01() < state.Level2Chance ? 2 : 1;
            var item = new ItemInstance(type, level);
            board.TryPlaceItem(cellIndex, item);

            // If the generator was full, its recovery interval starts counting now.
            if (state.Energy >= state.MaxEnergy)
                state.LastRecoveryUnix = nowUnix;
            state.Energy--;

            StatesChanged?.Invoke();
            return new SpawnResult(SpawnResultCode.Ok, cellIndex, item);
        }

        public void RaiseStatesChanged()
        {
            StatesChanged?.Invoke();
        }
    }
}
