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
    /// Handles the drag life cycle (webGL_game.md §15): a translucent, slightly
    /// enlarged ghost token follows the pointer on the top-most DragLayer, valid
    /// drop cells highlight, and failed drops fly back to the origin with a shake.
    ///
    /// Robustness rules (from adversarial review):
    /// - Only the pointer that started the drag may update/end it (multi-touch,
    ///   extra mouse buttons are ignored).
    /// - The dragged item reference is captured at begin; if the board changed
    ///   underneath (e.g. an order consumed the item mid-drag) the drop is cancelled.
    /// - The return animation owns its ghost via a captured local, so overlapping
    ///   drags can never destroy each other's ghosts.
    /// </summary>
    public sealed class DragController : MonoBehaviour, IBoardDragHandler
    {
        private const int NoPointer = int.MinValue;

        private GameManager _game;
        private BoardGridView _grid;
        private RectTransform _layer;

        private int _fromIndex = -1;
        private int _pointerId = NoPointer;
        private ItemInstance _draggedItem;
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

        public void OnCellBeginDrag(BoardCell cell, PointerEventData eventData)
        {
            if (_fromIndex >= 0)
                return;
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            ItemInstance item = _game.Board.GetItem(cell.Index);
            if (item == null)
                return;

            _fromIndex = cell.Index;
            _pointerId = eventData.pointerId;
            _draggedItem = item;

            // Hide the original token for the whole drag; the grid keeps it hidden
            // even if unrelated refreshes hit this cell meanwhile.
            _grid.SetSuppressedTokenIndex(cell.Index);

            CreateGhost(item, cell.Rect.rect.size);
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
            if (hover != null && MergeResolver.CanDrop(_game.Board, _fromIndex, hover.Index))
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
            ItemInstance dragged = _draggedItem;
            RectTransform ghost = _ghost;
            _fromIndex = -1;
            _pointerId = NoPointer;
            _draggedItem = null;
            _ghost = null;
            _grid.SetSuppressedTokenIndex(-1);

            BoardCell target = CellUnder(eventData);

            MoveOutcome outcome;
            if (_game.Board.GetItem(from) != dragged)
            {
                // The board changed mid-drag (an order consumed the item, ...). Never
                // move whatever occupies the origin cell now.
                outcome = MoveOutcome.InvalidTarget;
                target = null;
            }
            else
            {
                outcome = target == null
                    ? MoveOutcome.InvalidTarget
                    : _game.RequestMove(from, target.Index);
            }

            if (outcome == MoveOutcome.MovedToEmpty || outcome == MoveOutcome.Merged)
            {
                if (ghost != null)
                    Destroy(ghost.gameObject);
            }
            else
            {
                StartCoroutine(ReturnGhostRoutine(ghost, from, target, outcome));
            }
        }

        private IEnumerator ReturnGhostRoutine(RectTransform ghost, int from, BoardCell target,
            MoveOutcome outcome)
        {
            if (target != null &&
                (outcome == MoveOutcome.RejectedIncompatible || outcome == MoveOutcome.RejectedMaxLevel))
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
                _grid.RefreshCell(from); // re-shows the token (unless a new drag suppresses it)
                _grid.PlayShake(from);
            }
        }

        private void CreateGhost(ItemInstance item, Vector2 size)
        {
            ItemDefinition def = item.Definition;

            RectTransform ghost = UIFactory.CreateUiObject(_layer, "DragGhost");
            ghost.anchorMin = ghost.anchorMax = new Vector2(0.5f, 0.5f);
            ghost.sizeDelta = size;
            ghost.localScale = Vector3.one * 1.1f;

            var group = ghost.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0.85f;
            group.blocksRaycasts = false;
            group.interactable = false;

            Image circle = UIFactory.CreateImage(ghost, "Circle", def.Color);
            circle.sprite = SpriteFactory.Circle;
            circle.raycastTarget = false;
            var circleRect = (RectTransform)circle.transform;
            UIFactory.Stretch(circleRect);
            circleRect.offsetMin = new Vector2(8f, 8f);
            circleRect.offsetMax = new Vector2(-8f, -8f);

            Text label = UIFactory.CreateText(circle.transform, "Label", def.ShortLabel, 44,
                UITheme.LabelOn(def.Color), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch((RectTransform)label.transform);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = 46;

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
