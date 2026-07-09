using System.Collections;
using MergeCafe.Board;
using MergeCafe.Core;
using MergeCafe.Data;
using MergeCafe.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MergeCafe.Items
{
    /// <summary>
    /// Drag / tap handling for the board (webGL_game.md §15):
    /// - Dragging an item moves or merges it.
    /// - Dragging a generator tile moves it to a free cell (never merges).
    /// - Tapping a generator tile (no drag) produces an item.
    /// A translucent ghost follows the pointer; failed drags fly back and shake.
    /// Only the pointer that started the drag can update/end it.
    /// </summary>
    public sealed class DragController : MonoBehaviour, IBoardDragHandler
    {
        private const int NoPointer = int.MinValue;

        private GameManager _game;
        private BoardGridView _grid;
        private RectTransform _layer;

        private int _fromIndex = -1;
        private int _pointerId = NoPointer;
        private bool _draggingGenerator;
        private ItemInstance _draggedItem;
        private ItemType _draggedGenerator;
        private RectTransform _ghost;
        private BoardCell _hoverCell;

        public static DragController Build(RectTransform dragLayer, GameManager game, BoardGridView grid)
        {
            var controller = dragLayer.gameObject.AddComponent<DragController>();
            controller._layer = dragLayer;
            controller._game = game;
            controller._grid = grid;
            return controller;
        }

        public void OnCellClick(BoardCell cell, PointerEventData eventData)
        {
            // Tap on a generator tile → produce near it (drags don't fire click).
            if (_game.Board.HasGenerator(cell.Index))
                _game.RequestSpawn(_game.Board.GetGenerator(cell.Index), cell.Index, TimeUtil.NowUnixSeconds());
        }

        public void OnCellBeginDrag(BoardCell cell, PointerEventData eventData)
        {
            if (_fromIndex >= 0 || eventData.button != PointerEventData.InputButton.Left)
                return;

            if (_game.Board.HasGenerator(cell.Index))
            {
                _draggingGenerator = true;
                _draggedGenerator = _game.Board.GetGenerator(cell.Index);
            }
            else
            {
                ItemInstance item = _game.Board.GetItem(cell.Index);
                if (item == null)
                    return;
                _draggingGenerator = false;
                _draggedItem = item;
            }

            _fromIndex = cell.Index;
            _pointerId = eventData.pointerId;
            _grid.SetSuppressedTokenIndex(cell.Index);
            CreateGhost(cell.Rect.rect.size);
            MoveGhost(eventData);
        }

        public void OnCellDrag(PointerEventData eventData)
        {
            if (_fromIndex < 0 || eventData.pointerId != _pointerId)
                return;

            MoveGhost(eventData);

            BoardCell hover = CellUnder(eventData);
            if (hover == _hoverCell)
                return;

            ClearHover();
            if (hover != null && CanDropOn(hover.Index))
            {
                _hoverCell = hover;
                hover.SetVisual(CellVisual.Highlight);
            }
        }

        public void OnCellEndDrag(PointerEventData eventData)
        {
            if (_fromIndex < 0 || eventData.pointerId != _pointerId)
                return;

            ClearHover();

            int from = _fromIndex;
            RectTransform ghost = _ghost;
            _fromIndex = -1;
            _pointerId = NoPointer;
            _ghost = null;
            _grid.SetSuppressedTokenIndex(-1);

            BoardCell target = CellUnder(eventData);
            bool success = TryApply(from, target);

            if (success)
            {
                // Pop feedback is driven by the ItemMerged / ItemSpawned events.
                if (ghost != null)
                    Destroy(ghost.gameObject);
            }
            else
            {
                StartCoroutine(ReturnGhostRoutine(ghost, from, target));
            }
        }

        private bool TryApply(int from, BoardCell target)
        {
            if (target == null)
                return false;

            if (_draggingGenerator)
            {
                // The generator must still be where the drag started.
                if (!_game.Board.HasGenerator(from) || _game.Board.GetGenerator(from) != _draggedGenerator)
                    return false;
                return _game.RequestMoveGenerator(from, target.Index);
            }

            if (_game.Board.GetItem(from) != _draggedItem)
                return false; // board changed underneath (e.g. order consumed it)

            MoveOutcome outcome = _game.RequestMoveItem(from, target.Index);
            return outcome == MoveOutcome.MovedToEmpty || outcome == MoveOutcome.Merged;
        }

        private bool CanDropOn(int toIndex)
        {
            if (_draggingGenerator)
                return _game.Board.IsFreeCell(toIndex);
            return MergeResolver.CanDrop(_game.Board, _fromIndex, toIndex);
        }

        private IEnumerator ReturnGhostRoutine(RectTransform ghost, int from, BoardCell target)
        {
            // Item rejected by an occupied / generator cell → red flash on the target.
            if (target != null && target.Index != from &&
                (_game.Board.GetItem(target.Index) != null || _game.Board.HasGenerator(target.Index)))
            {
                _grid.FlashReject(target.Index);
            }

            BoardCell origin = _grid.GetCell(from);
            if (ghost != null && origin != null)
            {
                Vector2 start = ghost.anchoredPosition;
                Vector2 end = LocalPointOf(origin.Rect);
                const float duration = 0.12f;
                for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
                {
                    if (ghost == null)
                        break;
                    ghost.anchoredPosition = Vector2.Lerp(start, end, t / duration);
                    yield return null;
                }
            }

            if (ghost != null)
                Destroy(ghost.gameObject);

            if (origin != null)
            {
                _grid.RefreshCell(from);
                _grid.PlayShake(from);
            }
        }

        private void CreateGhost(Vector2 size)
        {
            RectTransform ghost = UIFactory.CreateUiObject(_layer, "DragGhost");
            ghost.anchorMin = ghost.anchorMax = new Vector2(0.5f, 0.5f);
            ghost.sizeDelta = size;
            ghost.localScale = Vector3.one * 1.1f;

            var group = ghost.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0.85f;
            group.blocksRaycasts = false;
            group.interactable = false;

            if (_draggingGenerator)
            {
                Image plate = UIFactory.CreateImage(ghost, "Plate", new Color(0.16f, 0.13f, 0.10f, 0.65f));
                plate.sprite = SpriteFactory.RoundedRect;
                plate.type = Image.Type.Sliced;
                plate.raycastTarget = false;
                UIFactory.Stretch((RectTransform)plate.transform);

                Image icon = UIFactory.CreateImage(plate.transform, "Icon", Color.white);
                icon.sprite = FoodIcons.Generator(_draggedGenerator);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UIFactory.Stretch((RectTransform)icon.transform);
                ((RectTransform)icon.transform).offsetMin = new Vector2(6f, 6f);
                ((RectTransform)icon.transform).offsetMax = new Vector2(-6f, -6f);
            }
            else
            {
                ItemDefinition def = _draggedItem.Definition;
                Image disc = UIFactory.CreateImage(ghost, "Disc",
                    new Color(def.Color.r, def.Color.g, def.Color.b, 0.28f));
                disc.sprite = SpriteFactory.Circle;
                disc.raycastTarget = false;
                UIFactory.Stretch((RectTransform)disc.transform);

                Image icon = UIFactory.CreateImage(disc.transform, "Icon", Color.white);
                icon.sprite = FoodIcons.Item(_draggedItem.Type, _draggedItem.Level);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UIFactory.Stretch((RectTransform)icon.transform);
                ((RectTransform)icon.transform).offsetMin = new Vector2(8f, 8f);
                ((RectTransform)icon.transform).offsetMax = new Vector2(-8f, -8f);
            }

            _ghost = ghost;
        }

        private void MoveGhost(PointerEventData eventData)
        {
            if (_ghost == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _layer, eventData.position, eventData.pressEventCamera, out Vector2 local);
            _ghost.anchoredPosition = local;
        }

        private Vector2 LocalPointOf(RectTransform rect)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null,
                rect.TransformPoint(rect.rect.center));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, null, out Vector2 local);
            return local;
        }

        private static BoardCell CellUnder(PointerEventData eventData)
        {
            GameObject hit = eventData.pointerCurrentRaycast.gameObject;
            return hit != null ? hit.GetComponentInParent<BoardCell>() : null;
        }

        private void ClearHover()
        {
            if (_hoverCell == null)
                return;
            _grid.RefreshCell(_hoverCell.Index);
            _hoverCell = null;
        }
    }
}
