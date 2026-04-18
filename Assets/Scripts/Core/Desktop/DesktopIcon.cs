using System.Collections;
using LudumDare2026.Core.Windows;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LudumDare2026.Core.Desktop
{
    public class DesktopIcon : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerClickHandler
    {
        [SerializeField] private RectTransform _dragRoot;
        [SerializeField] private Canvas _rootCanvas;
        [FormerlySerializedAs("_targetWindow")]
        [SerializeField] private DesktopWindow _windowController;
        [FormerlySerializedAs("_windowSlotIndex")]
        [SerializeField] private DesktopAppKind _appKind;
        [SerializeField] private float _dragThresholdPixels = 8f;

        [Header("Icon click feedback")]
        [SerializeField] private Image _iconImage;
        [Tooltip("If set, this sprite is restored after the pressed flash; otherwise the sprite on the Image at startup is used.")]
        [SerializeField] private Sprite _defaultIconSprite;
        [SerializeField] private Sprite _pressedSprite;
        [SerializeField] private float _pressedFeedbackDuration = 0.15f;

        private Vector2 _pressScreenPosition;
        private bool _dragCommitted;
        private Sprite _cachedDefaultSprite;
        private Coroutine _restoreIconRoutine;

        private void Awake()
        {
            if (_iconImage == null)
                _iconImage = GetComponent<Image>();

            if (_iconImage != null)
                _cachedDefaultSprite = _defaultIconSprite != null ? _defaultIconSprite : _iconImage.sprite;
        }

        private void OnDestroy()
        {
            if (_restoreIconRoutine != null)
                StopCoroutine(_restoreIconRoutine);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressScreenPosition = eventData.position;
            _dragCommitted = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragCommitted && Vector2.Distance(eventData.position, _pressScreenPosition) >= _dragThresholdPixels)
                _dragCommitted = true;

            if (!_dragCommitted)
                return;

            if (_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                _dragRoot.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
                return;
            }

            var canvasRect = _rootCanvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, _rootCanvas.worldCamera, out var localPoint)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position - eventData.delta, _rootCanvas.worldCamera, out var prevLocal))
            {
                _dragRoot.anchoredPosition += localPoint - prevLocal;
            }
        }

        /// <summary>
        /// Uses UI click counting plus a drag threshold so a double-click is not eaten by a tiny move.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_dragCommitted)
                return;

            PlayPressedIconFeedback();

            if (eventData.clickCount < 2)
                return;

            _windowController.OpenOrRestoreFromIcon(_appKind);
        }

        private void PlayPressedIconFeedback()
        {
            if (_iconImage == null || _pressedSprite == null || _pressedFeedbackDuration <= 0f)
                return;

            if (_restoreIconRoutine != null)
                StopCoroutine(_restoreIconRoutine);

            _iconImage.sprite = _pressedSprite;
            _restoreIconRoutine = StartCoroutine(RestoreDefaultIconAfterDelay());
        }

        private IEnumerator RestoreDefaultIconAfterDelay()
        {
            yield return new WaitForSecondsRealtime(_pressedFeedbackDuration);

            if (_iconImage != null)
                _iconImage.sprite = _cachedDefaultSprite;

            _restoreIconRoutine = null;
        }
    }
}
