using System;
using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Generators;
using MergeCafe.Items;
using UnityEngine;

namespace MergeCafe.Core
{
    /// <summary>
    /// Central coordinator (pure C#): owns the systems and routes player intents
    /// to them, emitting UI-facing events (toasts, spawn feedback).
    /// </summary>
    public sealed class GameManager
    {
        public BoardManager Board { get; }
        public GeneratorManager Generators { get; }

        /// <summary>Short user-facing message (space/energy warnings...).</summary>
        public event Action<string> ToastRequested;

        /// <summary>Cell index that just received a freshly generated item.</summary>
        public event Action<int> ItemSpawned;

        /// <summary>Cell index holding the item that was just upgraded by a merge.</summary>
        public event Action<int> ItemMerged;

        /// <summary>(from, to) after a simple move to an empty cell.</summary>
        public event Action<int, int> ItemMoved;

        public GameManager(double nowUnix, Func<float> rng01 = null)
        {
            Board = new BoardManager();
            Generators = new GeneratorManager(nowUnix, rng01);
        }

        public void Tick(double nowUnix)
        {
            Generators.Tick(nowUnix);
        }

        /// <summary>Player clicked a generator button (webGL_game.md §11 생성 규칙).</summary>
        public bool RequestSpawn(ItemType type, double nowUnix)
        {
            SpawnResult result = Generators.TrySpawn(type, Board, nowUnix);
            switch (result.Code)
            {
                case SpawnResultCode.Ok:
                    ItemSpawned?.Invoke(result.CellIndex);
                    return true;

                case SpawnResultCode.BoardFull:
                    Toast("보드 공간이 부족합니다");
                    return false;

                case SpawnResultCode.NoEnergy:
                    int seconds = Mathf.CeilToInt(
                        (float)Generators.Get(type).SecondsToNextRecovery(nowUnix));
                    Toast($"에너지가 부족합니다 ({seconds}초 후 +1)");
                    return false;

                default:
                    Toast($"{GeneratorCatalog.For(type).UnlockCost} 골드로 해금할 수 있습니다");
                    return false;
            }
        }

        /// <summary>Player dropped the item at fromIndex onto toIndex (webGL_game.md §9).</summary>
        public MoveOutcome RequestMove(int fromIndex, int toIndex)
        {
            MoveOutcome outcome = MergeResolver.Resolve(Board, fromIndex, toIndex);
            switch (outcome)
            {
                case MoveOutcome.Merged:
                    ItemMerged?.Invoke(toIndex);
                    break;
                case MoveOutcome.MovedToEmpty:
                    ItemMoved?.Invoke(fromIndex, toIndex);
                    break;
                case MoveOutcome.RejectedMaxLevel:
                    Toast("이미 최고 레벨입니다");
                    break;
            }
            return outcome;
        }

        internal void Toast(string message)
        {
            ToastRequested?.Invoke(message);
        }
    }
}
