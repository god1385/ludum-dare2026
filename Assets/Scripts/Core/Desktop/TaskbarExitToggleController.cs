using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare2026.Core.Desktop
{
    /// <summary>
    /// Static taskbar button toggles a second button sliding up/down (DOTween) between hidden and shown anchored positions.
    /// </summary>
    public class TaskbarExitToggleController : MonoBehaviour
    {
        [SerializeField] private Button _staticToggleButton;
        [SerializeField] private RectTransform _exitFlyButtonRoot;
        [Tooltip("Anchored position when the exit button is visible (flies here).")]
        [SerializeField] private Vector2 _shownAnchoredPosition;
        [Tooltip("Anchored position when hidden (e.g. below the screen).")]
        [SerializeField] private Vector2 _hiddenAnchoredPosition;
        [SerializeField] private float _duration = 0.35f;
        [SerializeField] private Ease _ease = Ease.OutCubic;

        private bool _expanded;
        private Tween _moveTween;

        private void Awake()
        {
            if (_exitFlyButtonRoot != null)
                _exitFlyButtonRoot.anchoredPosition = _hiddenAnchoredPosition;

            if (_staticToggleButton != null)
                _staticToggleButton.onClick.AddListener(OnStaticToggleClicked);
        }

        private void OnDestroy()
        {
            if (_staticToggleButton != null)
                _staticToggleButton.onClick.RemoveListener(OnStaticToggleClicked);

            _moveTween?.Kill();
        }

        private void OnStaticToggleClicked()
        {
            if (_exitFlyButtonRoot == null)
                return;

            if (_moveTween != null && _moveTween.IsActive())
                return;

            _expanded = !_expanded;
            var target = _expanded ? _shownAnchoredPosition : _hiddenAnchoredPosition;
            _moveTween = _exitFlyButtonRoot
                .DOAnchorPos(target, _duration)
                .SetEase(_ease)
                .SetUpdate(true);
        }
    }
}
