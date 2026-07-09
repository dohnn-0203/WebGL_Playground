using UnityEngine;

namespace MergeCafe.UI
{
    /// <summary>
    /// References to the runtime-built root panels. New layout: a left column holds
    /// the shared energy gauge and the order cards, the board fills the rest, and a
    /// slim bottom bar holds the upgrades. There is no right panel.
    /// </summary>
    public sealed class UiLayout
    {
        public Canvas Canvas;
        public RectTransform Root;

        public RectTransform TopHud;
        public RectTransform LeftPanel;
        public RectTransform BoardPanel;
        public RectTransform BottomBar;

        public RectTransform DragLayer;
        public RectTransform ToastLayer;
        public RectTransform PopupLayer;
    }
}
