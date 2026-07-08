using MergeCafe.Data;
using MergeCafe.Items;
using MergeCafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.Board
{
    /// <summary>
    /// Builds and refreshes the 6x6 cell grid inside the board panel.
    /// The grid lives in a centered square container (AspectRatioFitter) so it
    /// survives browser window resizes (webGL_game.md §8).
    /// </summary>
    public sealed class BoardGridView : MonoBehaviour
    {
        private BoardManager _board;
        private readonly BoardCell[] _cells = new BoardCell[BoardManager.CellCount];

        public RectTransform Container => (RectTransform)transform;

        public static BoardGridView Build(RectTransform boardPanel, BoardManager board)
        {
            // Padding wrapper, then a square container fitted inside it.
            RectTransform padding = UIFactory.CreateUiObject(boardPanel, "BoardPadding");
            padding.anchorMin = Vector2.zero;
            padding.anchorMax = Vector2.one;
            padding.offsetMin = new Vector2(24f, 24f);
            padding.offsetMax = new Vector2(-24f, -24f);

            RectTransform container = UIFactory.CreateUiObject(padding, "BoardContainer");
            var fitter = container.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            var view = container.gameObject.AddComponent<BoardGridView>();
            view._board = board;

            for (int i = 0; i < BoardManager.CellCount; i++)
                view._cells[i] = BoardCell.Create(container, i);

            board.CellChanged += view.OnCellChanged;
            view.RefreshAll();
            return view;
        }

        public BoardCell GetCell(int index)
        {
            return index >= 0 && index < _cells.Length ? _cells[index] : null;
        }

        public void RefreshAll()
        {
            for (int i = 0; i < _cells.Length; i++)
                RefreshCell(i);
        }

        private void OnCellChanged(int index)
        {
            RefreshCell(index);
        }

        private void RefreshCell(int index)
        {
            BoardCell cell = _cells[index];
            if (cell == null)
                return;

            cell.SetVisual(_board.IsUnlocked(index) ? CellVisual.Open : CellVisual.Locked);

            // Keep the token child in sync with the board model.
            ItemInstance item = _board.GetItem(index);
            if (item != null)
                ItemTokenView.CreateOrUpdate(cell, item);
            else
                ItemTokenView.RemoveFrom(cell);
        }

        private void OnDestroy()
        {
            if (_board != null)
                _board.CellChanged -= OnCellChanged;
        }
    }
}
