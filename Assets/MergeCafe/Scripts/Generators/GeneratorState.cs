using System;
using MergeCafe.Data;

namespace MergeCafe.Generators
{
    /// <summary>
    /// Mutable runtime state of one generator: unlock, upgrade level, energy and
    /// the wall-clock recovery anchor. Pure C#, fully unit-testable.
    /// </summary>
    public sealed class GeneratorState
    {
        public GeneratorDefinition Definition { get; }

        public bool Unlocked { get; set; }
        public int UpgradeLevel { get; set; } = 1;
        public int Energy { get; set; }

        /// <summary>Unix time (seconds) the current recovery interval started.</summary>
        public double LastRecoveryUnix { get; set; }

        public int MaxEnergy => Definition.BaseMaxEnergy + GeneratorCatalog.EnergyBonus(UpgradeLevel);

        public float Level2Chance => GeneratorCatalog.Level2Chance(UpgradeLevel);

        public GeneratorState(GeneratorDefinition definition, double nowUnix)
        {
            Definition = definition;
            Unlocked = definition.UnlockCost == 0;
            Energy = definition.BaseMaxEnergy;
            LastRecoveryUnix = nowUnix;
        }

        /// <summary>
        /// Applies wall-clock energy recovery (+1 per RecoverySeconds, webGL_game.md §11).
        /// Works across page reloads because the anchor is persisted with the save data.
        /// </summary>
        public void Recover(double nowUnix)
        {
            if (Energy >= MaxEnergy)
            {
                LastRecoveryUnix = nowUnix;
                return;
            }

            double elapsed = nowUnix - LastRecoveryUnix;
            if (elapsed < 0)
            {
                // Clock moved backwards (system clock change) — restart the interval.
                LastRecoveryUnix = nowUnix;
                return;
            }

            int gained = (int)(elapsed / Definition.RecoverySeconds);
            if (gained <= 0)
                return;

            int applied = Math.Min(gained, MaxEnergy - Energy);
            Energy += applied;
            LastRecoveryUnix = Energy >= MaxEnergy
                ? nowUnix
                : LastRecoveryUnix + (double)gained * Definition.RecoverySeconds;
        }

        public double SecondsToNextRecovery(double nowUnix)
        {
            if (Energy >= MaxEnergy)
                return 0;
            return Math.Max(0, Definition.RecoverySeconds - (nowUnix - LastRecoveryUnix));
        }
    }
}
