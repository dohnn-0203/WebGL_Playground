using MergeCafe.Board;
using MergeCafe.Data;

namespace MergeCafe.Items
{
    public enum MoveOutcome
    {
        /// <summary>Drop landed outside the board / source missing.</summary>
        InvalidTarget,
        SameCell,
        MovedToEmpty,
        Merged,
        RejectedLocked,
        RejectedIncompatible,
        RejectedMaxLevel
    }

    /// <summary>
    /// Pure merge/move rules of webGL_game.md §9. Mutates the board only for
    /// successful outcomes; every rejection leaves the board untouched.
    /// </summary>
    public static class MergeResolver
    {
        /// <summary>source.itemType == target.itemType && source.level == target.level && source.level < maxLevel</summary>
        public static bool CanMerge(ItemInstance source, ItemInstance target)
        {
            return source != null && target != null
                && source.Type == target.Type
                && source.Level == target.Level
                && source.Level < ItemCatalog.MaxLevel;
        }

        /// <summary>Would dropping the item at fromIndex onto toIndex do something? (drag highlight)</summary>
        public static bool CanDrop(BoardManager board, int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex)
                return false;

            ItemInstance source = board.GetItem(fromIndex);
            if (source == null || !board.IsUnlocked(toIndex))
                return false;

            ItemInstance target = board.GetItem(toIndex);
            return target == null || CanMerge(source, target);
        }

        /// <summary>Applies a drag of the item at fromIndex onto toIndex.</summary>
        public static MoveOutcome Resolve(BoardManager board, int fromIndex, int toIndex)
        {
            ItemInstance source = board.GetItem(fromIndex);
            if (source == null || !board.IsValidIndex(toIndex))
                return MoveOutcome.InvalidTarget;

            if (fromIndex == toIndex)
                return MoveOutcome.SameCell;

            if (!board.IsUnlocked(toIndex))
                return MoveOutcome.RejectedLocked;

            ItemInstance target = board.GetItem(toIndex);
            if (target == null)
            {
                board.RemoveItem(fromIndex);
                board.TryPlaceItem(toIndex, source);
                return MoveOutcome.MovedToEmpty;
            }

            if (CanMerge(source, target))
            {
                board.RemoveItem(fromIndex);
                board.RemoveItem(toIndex);
                board.TryPlaceItem(toIndex, new ItemInstance(source.Type, source.Level + 1));
                return MoveOutcome.Merged;
            }

            bool sameKindAtMax = source.Type == target.Type && source.Level == target.Level;
            return sameKindAtMax ? MoveOutcome.RejectedMaxLevel : MoveOutcome.RejectedIncompatible;
        }
    }
}
