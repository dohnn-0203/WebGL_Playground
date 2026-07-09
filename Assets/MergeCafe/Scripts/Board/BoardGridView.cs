using System.Collections;
using MergeCafe.Data;
using MergeCafe.Items;
using MergeCafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.Board
{
    /// <summary>
    /// Builds and refreshes the Cols×Rows cell grid inside the board panel.
    /// The grid lives in a centered aspect-fitted container so it survives browser
    /// window resizes (webGL_game.md §8).
    /// </summary>
    public sealed class BoardGridView : MonoBehaviour
    {
        private BoardManager _board;
        private readonly BoardCell[] _cells = new BoardCell[BoardManager.CellCount];
        private int _suppressedTokenIndex = -1;

        public RectTransform Container => (RectTransform)transform;

        /// <summary>
        /// While a drag is active, the origin cell's token stays hidden no matter
        /// which code path refreshes that cell. Pass -1 to clear.
        /// </summary>
        public void SetSuppressedTokenIndex(int index)
        {
            int previous = _suppressedTokenIndex;
            _suppressedTokenIndex = index;
            if (previous >= 0 && previous != index)
                RefreshCell(previous);
            if (index >= 0)
                RefreshCell(index);
        }

        public static BoardGridView Build(RectTransform boardPanel, BoardManager board)
        {
            // Padding wrapper, then a square container fitted inside it.
            RectTransform padding = UIFactory.CreateUiObject(boardPanel, "BoardPadding");
            padding.anchorMin = Vector2.zero;
            padding.anchorMax = Vector2.one;
            padding.offsetMin = new Vector2(24f, 24f);
            padding.offsetMax = new Vector2(-24f, -24f);

            RectTransform container = UIFactory.CreateUiObject(padding, "BoardContainer");
            var fitter = container.gameObject.AddComponent<BoardAspectFitter>();
            fitter.Aspect = BoardManager.Cols / (float)BoardManager.Rows;

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

            bool suppressed = index == _suppressedTokenIndex;

            // A cell holds a generator, an item, or nothing — keep the child views in sync.
            if (_board.HasGenerator(index))
            {
                ItemTokenView.RemoveFrom(cell);
                GeneratorTileView tile = GeneratorTileView.CreateOrUpdate(cell, _board.GetGenerator(index));
                tile.gameObject.SetActive(!suppressed);
            }
            else
            {
                GeneratorTileView.RemoveFrom(cell);
                ItemInstance item = _board.GetItem(index);
                if (item != null)
                {
                    ItemTokenView token = ItemTokenView.CreateOrUpdate(cell, item);
                    token.gameObject.SetActive(!suppressed);
                }
                else
                {
                    ItemTokenView.RemoveFrom(cell);
                }
            }
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
            if (cell == null)
                return null;
            // Whichever occupant view is present (item token or generator tile).
            Transform token = cell.transform.Find(ItemTokenView.TokenName);
            return token != null ? token : cell.transform.Find(GeneratorTileView.TileName);
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
            // A token at rest always sits at anchoredPosition (0,0) (symmetric stretch
            // offsets), so overlapping shakes can safely restore to zero.
            var rect = (RectTransform)token;
            const float duration = 0.22f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (rect == null)
                    yield break;
                float offset = Mathf.Sin(t * 60f) * 6f * (1f - t / duration);
                rect.anchoredPosition = new Vector2(offset, 0f);
                yield return null;
            }
            if (rect != null)
                rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>Expanding white flash over a cell — merge success feedback (§16).</summary>
        public void PlayMergeFlash(int index)
        {
            BoardCell cell = GetCell(index);
            if (cell != null)
                StartCoroutine(MergeFlashRoutine(cell));
        }

        private IEnumerator MergeFlashRoutine(BoardCell cell)
        {
            Image flash = UIFactory.CreateImage(cell.transform, "MergeFlash", Color.white);
            flash.sprite = SpriteFactory.Circle;
            flash.raycastTarget = false;
            var rect = (RectTransform)flash.transform;
            UIFactory.Stretch(rect);
            rect.offsetMin = new Vector2(6f, 6f);
            rect.offsetMax = new Vector2(-6f, -6f);

            const float duration = 0.28f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (flash == null)
                    yield break;
                float p = t / duration;
                flash.color = new Color(1f, 1f, 1f, 0.8f * (1f - p));
                flash.transform.localScale = Vector3.one * (1f + 0.45f * p);
                yield return null;
            }
            if (flash != null)
                Destroy(flash.gameObject);
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
