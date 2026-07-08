using UnityEngine;

namespace MergeCafe.UI
{
    /// <summary>
    /// References to the runtime-built root panels, matching the layout in webGL_game.md §7-8.
    /// Filled by <see cref="UIFactory.BuildBaseLayout"/> and handed to the game systems.
    /// </summary>
    public sealed class UiLayout
    {
        public Canvas Canvas;
        public RectTransform Root;

        public RectTransform TopHud;
        public RectTransform GeneratorPanel;
        public RectTransform BoardPanel;
        public RectTransform OrderPanel;
        public RectTransform UpgradePanel;

        /// <summary>Full-screen layer that dragged items are re-parented to (always on top of panels).</summary>
        public RectTransform DragLayer;

        /// <summary>Full-screen layer for toast messages.</summary>
        public RectTransform ToastLayer;

        /// <summary>Full-screen layer for modal popups (confirm dialogs).</summary>
        public RectTransform PopupLayer;
    }
}
