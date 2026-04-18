using UnityEngine;

namespace LudumDare2026.Utilities
{
    public struct WindowLayoutSnapshot
    {
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;
        public Vector3 AnchoredPosition3D;
        public Vector2 SizeDelta;
        public Vector3 LocalScale;

        public static WindowLayoutSnapshot Capture(RectTransform rectTransform) => new WindowLayoutSnapshot
        {
            AnchorMin = rectTransform.anchorMin,
            AnchorMax = rectTransform.anchorMax,
            Pivot = rectTransform.pivot,
            AnchoredPosition3D = rectTransform.anchoredPosition3D,
            SizeDelta = rectTransform.sizeDelta,
            LocalScale = rectTransform.localScale,
        };

        public readonly void Apply(RectTransform rectTransform)
        {
            rectTransform.anchorMin = AnchorMin;
            rectTransform.anchorMax = AnchorMax;
            rectTransform.pivot = Pivot;
            rectTransform.anchoredPosition3D = AnchoredPosition3D;
            rectTransform.sizeDelta = SizeDelta;
            rectTransform.localScale = LocalScale;
        }
    }
}
