using System.Collections;
using System.Collections.Generic;
using LudumDare2026.Core.GameFlow;
using LudumDare2026.Core.Windows;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LudumDare2026.Core.Desktop
{
    public class TerminalNotificationIndicator : MonoBehaviour
    {
        [SerializeField] private DesktopWindow _desktopWindow;
        [SerializeField] private DesktopAppKind _appKind = DesktopAppKind.Terminal;
        [SerializeField] private GameObject _notificationRoot;
        [SerializeField] private Image _notificationImage;
        [SerializeField] private Sprite _frameA;
        [SerializeField] private Sprite _frameB;
        [SerializeField] private Sprite _frameC;
        [SerializeField] private float _frameIntervalSeconds = 0.35f;
        [SerializeField] private AudioSource _notificationAudioSource;

        private static readonly List<TerminalNotificationIndicator> Instances = new List<TerminalNotificationIndicator>();

        private IGameFlowPresentationModel _presentation;
        private CompositeDisposable _disposables;
        private Coroutine _blinkRoutine;

        [Inject]
        private void Construct([InjectOptional] IGameFlowPresentationModel presentation) => _presentation = presentation;

        private void OnEnable() => Instances.Add(this);

        private void OnDisable()
        {
            Instances.Remove(this);
            StopBlink();
        }

        private void Start()
        {
            ResolvePresentationIfNeeded();
            if (_presentation == null)
            {
                Debug.LogWarning($"{nameof(TerminalNotificationIndicator)}: no {nameof(IGameFlowPresentationModel)}.", this);
                return;
            }

            if (_notificationImage == null && _notificationRoot != null)
                _notificationImage = _notificationRoot.GetComponent<Image>()
                    ?? _notificationRoot.GetComponentInChildren<Image>(true);

            _disposables = new CompositeDisposable();
            _presentation.CurrentTerminalMessage.Subscribe(OnTerminalMessageChanged).AddTo(_disposables);
        }

        private void OnDestroy()
        {
            StopBlink();
            _disposables?.Dispose();
        }

        private void ResolvePresentationIfNeeded()
        {
            if (_presentation != null)
                return;

            var controller = Object.FindAnyObjectByType<GameFlowController>();
            if (controller != null)
                _presentation = controller.Presentation;
        }

        private void OnTerminalMessageChanged(CipherMessageData message)
        {
            if (message == null)
            {
                Hide();
                return;
            }

            if (_desktopWindow != null && _desktopWindow.IsWindowChromeActiveInHierarchy(_appKind))
            {
                Hide();
                return;
            }

            Show();
        }

        public static void NotifyTerminalOpened()
        {
            for (var i = Instances.Count - 1; i >= 0; i--)
            {
                var ind = Instances[i];
                if (ind != null)
                    ind.Hide();
            }
        }

        private void Show()
        {
            _notificationRoot?.SetActive(true);

            if (_notificationAudioSource != null)
                _notificationAudioSource.Play();

            if (_notificationImage == null || !HasAllFrames())
                return;

            StopBlink();
            _blinkRoutine = StartCoroutine(BlinkLoop());
        }

        private void Hide()
        {
            StopBlink();
            _notificationRoot?.SetActive(false);
        }

        private void StopBlink()
        {
            if (_blinkRoutine == null)
                return;

            StopCoroutine(_blinkRoutine);
            _blinkRoutine = null;
        }

        private bool HasAllFrames() => _frameA != null && _frameB != null && _frameC != null;

        private IEnumerator BlinkLoop()
        {
            var frames = new[] { _frameA, _frameB, _frameC };
            var i = 0;
            var wait = new WaitForSecondsRealtime(_frameIntervalSeconds);
            while (_notificationRoot != null && _notificationRoot.activeInHierarchy)
            {
                if (_notificationImage != null)
                    _notificationImage.sprite = frames[i % 3];
                i++;
                yield return wait;
            }

            _blinkRoutine = null;
        }
    }
}
