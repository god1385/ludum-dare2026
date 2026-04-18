using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare2026.Core.Windows
{
    public readonly struct DesktopWindowChromeOptions
    {
        public readonly string WindowTitle;

        public DesktopWindowChromeOptions(string windowTitle) =>
            WindowTitle = windowTitle ?? string.Empty;
    }

    public class DesktopWindowChrome : MonoBehaviour
    {
        [SerializeField] private RectTransform _layoutRoot;
        [SerializeField] private RectTransform _contentAnchor;
        [SerializeField] private TextMeshProUGUI _windowTitleLabel;
        [SerializeField] private Graphic _titleDragHandle;
        [SerializeField] private Button _minimizeButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        private IWindowChromeTarget _target;
        private Canvas _rootCanvas;
        private DesktopWindowChromeDrag _drag;

        public RectTransform LayoutRoot => _layoutRoot;

        public RectTransform ContentAnchor => _contentAnchor != null ? _contentAnchor : _layoutRoot;

        private void Awake() => _rootCanvas = _layoutRoot.GetComponentInParent<Canvas>();

        /// <summary>
        /// Wires chrome buttons to the host window and applies per-window chrome options.
        /// </summary>
        public void Bind(IWindowChromeTarget target, in DesktopWindowChromeOptions options)
        {
            _target = target;
            if (_windowTitleLabel != null)
                _windowTitleLabel.text = options.WindowTitle;

            _minimizeButton.onClick.AddListener(() => _target.RequestMinimize());

            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => _target.RequestClose());

            if (_canvasGroup != null)
                _canvasGroup.blocksRaycasts = true;

            SetupTitleDrag();
        }

        /// <summary>
        /// Clears listeners when the host window is destroyed or the chrome is re-bound.
        /// </summary>
        public void Unbind()
        {
            TeardownTitleDrag();
            _minimizeButton.onClick.RemoveAllListeners();
            if (_closeButton != null)
                _closeButton.onClick.RemoveAllListeners();
            _target = null;
        }

        public void SetInteractable(bool value)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.interactable = value;
            _canvasGroup.blocksRaycasts = value;
        }

        private void SetupTitleDrag()
        {
            if (_titleDragHandle == null || _layoutRoot == null)
                return;

            _drag = _titleDragHandle.gameObject.GetComponent<DesktopWindowChromeDrag>();
            if (_drag == null)
                _drag = _titleDragHandle.gameObject.AddComponent<DesktopWindowChromeDrag>();

            _drag.Attach(_layoutRoot, () => _target, _rootCanvas);
        }

        private void TeardownTitleDrag()
        {
            if (_drag == null)
                return;

            _drag.Detach();
            Destroy(_drag);
            _drag = null;
        }
    }

    /// <summary>
    /// Lives on the same GameObject as <see cref="DesktopWindowChrome._titleDragHandle"/> (needs raycast target).
    /// </summary>
    public sealed class DesktopWindowChromeDrag : MonoBehaviour, UnityEngine.EventSystems.IBeginDragHandler, UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IEndDragHandler
    {
        private RectTransform _moveRoot;
        private System.Func<IWindowChromeTarget> _resolveTarget;
        private Canvas _canvas;
        private bool _dragging;

        public void Attach(RectTransform moveRoot, System.Func<IWindowChromeTarget> resolveTarget, Canvas canvas)
        {
            _moveRoot = moveRoot;
            _resolveTarget = resolveTarget;
            _canvas = canvas;
        }

        public void Detach()
        {
            _dragging = false;
            _moveRoot = null;
            _resolveTarget = null;
            _canvas = null;
        }

        public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_moveRoot == null || _resolveTarget?.Invoke() == null)
                return;

            _dragging = true;
        }

        public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!_dragging || _moveRoot == null || _resolveTarget?.Invoke() == null || _canvas == null)
                return;

            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                _moveRoot.anchoredPosition += eventData.delta / _canvas.scaleFactor;
                return;
            }

            var canvasRect = _canvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, _canvas.worldCamera, out var localPoint)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position - eventData.delta, _canvas.worldCamera, out var prevLocal))
            {
                _moveRoot.anchoredPosition += localPoint - prevLocal;
            }
        }

        public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData) => _dragging = false;
    }
}
