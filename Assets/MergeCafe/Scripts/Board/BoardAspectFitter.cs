using UnityEngine;

namespace MergeCafe.Board
{
    /// <summary>
    /// Sizes this RectTransform to fit its parent while keeping a fixed aspect ratio,
    /// centered. Deliberately NOT an ILayoutController: it writes sizeDelta from Update
    /// (after layout), so it never participates in the CanvasUpdateRegistry layout pass.
    ///
    /// This replaces Unity's AspectRatioFitter (FitInParent), which can trigger a
    /// re-entrant layout rebuild → "Maximum call stack size exceeded" on WebGL.
    /// </summary>
    public sealed class BoardAspectFitter : MonoBehaviour
    {
        public float Aspect = 1f;

        private RectTransform _rect;
        private RectTransform _parent;
        private Vector2 _lastParentSize = new Vector2(-1f, -1f);

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _parent = transform.parent as RectTransform;
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = Vector2.zero;
        }

        private void OnEnable() => Fit();

        private void Update() => Fit();

        private void Fit()
        {
            if (_parent == null || Aspect <= 0f)
                return;

            Rect p = _parent.rect;
            if (p.width <= 0f || p.height <= 0f)
                return;

            // Only rewrite when the parent actually changed (avoids per-frame churn).
            var size = new Vector2(p.width, p.height);
            if (size == _lastParentSize)
                return;
            _lastParentSize = size;

            float fitW, fitH;
            if (p.width / p.height > Aspect)
            {
                fitH = p.height;
                fitW = p.height * Aspect;
            }
            else
            {
                fitW = p.width;
                fitH = p.width / Aspect;
            }

            _rect.sizeDelta = new Vector2(fitW, fitH);
            _rect.anchoredPosition = Vector2.zero;
        }
    }
}
