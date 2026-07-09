using System;

namespace MergeCafe.Generators
{
    /// <summary>
    /// A single shared energy resource used by every generator (the "total gauge").
    /// +1 every <see cref="RecoverySeconds"/>, on a wall clock so it keeps recovering
    /// across page reloads. Pure C#, fully unit-testable.
    /// </summary>
    public sealed class EnergyPool
    {
        public const int BaseMax = 20;
        public const int RecoverySeconds = 5;

        public int Current { get; set; }
        public int Max { get; set; }
        public double LastRecoveryUnix { get; set; }

        public event Action Changed;

        public EnergyPool(double nowUnix)
        {
            Max = BaseMax;
            Current = BaseMax;
            LastRecoveryUnix = nowUnix;
        }

        public bool HasEnergy => Current > 0;

        public bool TrySpend(double nowUnix)
        {
            if (Current <= 0)
                return false;

            // If we were full, the recovery interval starts counting from this spend.
            if (Current >= Max)
                LastRecoveryUnix = nowUnix;

            Current--;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Applies wall-clock recovery. Returns true if the amount changed.</summary>
        public bool Recover(double nowUnix)
        {
            if (Current >= Max)
            {
                LastRecoveryUnix = nowUnix;
                return false;
            }

            double elapsed = nowUnix - LastRecoveryUnix;
            if (elapsed < 0)
            {
                LastRecoveryUnix = nowUnix; // clock moved backwards
                return false;
            }

            int gained = (int)(elapsed / RecoverySeconds);
            if (gained <= 0)
                return false;

            int applied = Math.Min(gained, Max - Current);
            Current += applied;
            LastRecoveryUnix = Current >= Max
                ? nowUnix
                : LastRecoveryUnix + (double)gained * RecoverySeconds;

            Changed?.Invoke();
            return true;
        }

        public double SecondsToNextRecovery(double nowUnix)
        {
            if (Current >= Max)
                return 0;
            return Math.Max(0, RecoverySeconds - (nowUnix - LastRecoveryUnix));
        }

        public void RaiseChanged() => Changed?.Invoke();
    }
}
