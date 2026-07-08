using System.Collections;
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

        /// <summary>Routes cell drag events (begin/drag/end) to the given handler.</summary>
        public void SetDragHandler(IBoardDragHandler handler)
        {
            foreach (BoardCell cell in _cells)
                cell.SetDragHandler(handler);
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

        public void RefreshCell(int index)
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

        // ---- Small feedback animations (webGL_game.md §16) ----

        /// <summary>Scale pop on the token in the given cell (spawn / merge success).</summary>
        public void PlayPop(int index)
        {
            Transform token = TokenOf(index);
            if (token != null)
                StartCoroutine(PopRoutine(token));
        }

        /// <summary>Short horizontal shake on the token (failed move/merge).</summary>
        public void PlayShake(int index)
        {
            Transform token = TokenOf(index);
            if (token != null)
                StartCoroutine(ShakeRoutine(token));
        }

        /// <summary>Brief red flash on a cell that rejected a drop.</summary>
        public void FlashReject(int index)
        {
            BoardCell cell = GetCell(index);
            if (cell != null)
                StartCoroutine(RejectRoutine(cell));
        }

        private Transform TokenOf(int index)
        {
            BoardCell cell = GetCell(index);
            return cell != null ? cell.transform.Find(ItemTokenView.TokenName) : null;
        }

        private static IEnumerator PopRoutine(Transform token)
        {
            const float duration = 0.18f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (token == null)
                    yield break;
                float p = t / duration;
                // 0.6 → 1.08 → 1.0 overshoot curve
                float scale = p < 0.7f
                    ? Mathf.Lerp(0.6f, 1.08f, p / 0.7f)
                    : Mathf.Lerp(1.08f, 1f, (p - 0.7f) / 0.3f);
                token.localScale = Vector3.one * scale;
                yield return null;
            }
            if (token != null)
                token.localScale = Vector3.one;
        }

        private static IEnumerator ShakeRoutine(Transform token)
        {
            var rect = (RectTransform)token;
            Vector2 basePosition = rect.anchoredPosition;
            const float duration = 0.22f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (rect == null)
                    yield break;
                float offset = Mathf.Sin(t * 60f) * 6f * (1f - t / duration);
                rect.anchoredPosition = basePosition + new Vector2(offset, 0f);
                yield return null;
            }
            if (rect != null)
                rect.anchoredPosition = basePosition;
        }

        private IEnumerator RejectRoutine(BoardCell cell)
        {
            cell.SetVisual(CellVisual.Reject);
            yield return new WaitForSecondsRealtime(0.18f);
            if (cell != null)
                RefreshCell(cell.Index);
        }

        private void OnDestroy()
        {
            if (_board != null)
                _board.CellChanged -= OnCellChanged;
        }
    }
}
