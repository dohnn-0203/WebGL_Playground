using MergeCafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.Board
{
    public enum CellVisual
    {
        Locked,
        Open,
        Highlight,
        Reject
    }

    /// <summary>
    /// UI for a single board cell. Items dropped onto the board are resolved by
    /// raycasting against this cell's background image (webGL_game.md §15).
    /// </summary>
    public sealed class BoardCell : MonoBehaviour
    {
        public int Index { get; private set; }

        public RectTransform Rect => (RectTransform)transform;

        private Image _background;
        private CellVisual _visual;

        /// <summary>
        /// Creates one cell as a child of the (square) board container using fractional
        /// anchors, so the whole grid scales with the container without any layout code.
        /// </summary>
        public static BoardCell Create(RectTransform boardContainer, int index)
        {
            Image background = UIFactory.CreateImage(boardContainer, $"Cell_{index:D2}", UITheme.CellLocked);
            background.sprite = SpriteFactory.RoundedRect;
            background.type = Image.Type.Sliced;
            background.raycastTarget = true;

            int row = BoardManager.RowOf(index);
            int col = BoardManager.ColOf(index);

            var rect = (RectTransform)background.transform;
            rect.anchorMin = new Vector2(col / 6f, (5 - row) / 6f);
            rect.anchorMax = new Vector2((col + 1) / 6f, (6 - row) / 6f);
            rect.offsetMin = new Vector2(5f, 5f);
            rect.offsetMax = new Vector2(-5f, -5f);

            var cell = background.gameObject.AddComponent<BoardCell>();
            cell.Index = index;
            cell._background = background;
            cell.SetVisual(CellVisual.Locked);
            return cell;
        }

        public void SetVisual(CellVisual visual)
        {
            _visual = visual;
            switch (visual)
            {
                case CellVisual.Open:
                    _background.color = UITheme.CellOpen;
                    break;
                case CellVisual.Highlight:
                    _background.color = UITheme.CellHighlight;
                    break;
                case CellVisual.Reject:
                    _background.color = UITheme.CellReject;
                    break;
                default:
                    _background.color = UITheme.CellLocked;
                    break;
            }
        }

        public CellVisual Visual => _visual;
    }
}
