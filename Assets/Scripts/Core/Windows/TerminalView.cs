using System.Collections;
using System.Collections.Generic;
using System.Text;
using LudumDare2026.Core.Desktop;
using LudumDare2026.Core.GameFlow;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LudumDare2026.Core.Windows
{
    public class TerminalView : MonoBehaviour, IWindowTaskbarIconSource
    {
        private const string DefaultBadEndingCipherBlock =
            "PHNGLUI MGLWNAFH\nCTHULHU RLYEH\nWGAHNAGL FHTAGN";

        [SerializeField] private TextMeshProUGUI _output;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private float _secondsPerCharacter = 0.025f;
        [SerializeField] private float _pauseAfterLine = 0.12f;
        [SerializeField] private float _overflowPixels = 2f;
        [SerializeField] private float _scrollSensitivity = 35f;
        [SerializeField] private Sprite _taskbarIcon;
        [SerializeField] private Sprite _taskbarHighlightSprite;
        [SerializeField] private Sprite _taskbarPressedSprite;

        [Header("Alien message ambience")]
        [SerializeField] private AudioSource _alienAudioSource;
        [SerializeField] private AudioClip _alienAudioClip;

        [Header("Endings")]
        [SerializeField] private float _badEndingClockRampSeconds = 4f;
        [SerializeField] private float _badEndingPostDelaySeconds = 1f;
        [SerializeField] private float _goodEndingCreditsDelaySeconds = 5f;
        [SerializeField] private float _goodEndingLinePauseSeconds = 1f;

        private IGameFlowPresentationModel _presentation;
        private IEndingCipherMessages _endings;
        private GameExitFlowController _exitFlow;

        private DesktopWindow _desktopWindow;
        private DesktopClockWidget _clockWidget;

        private CompositeDisposable _disposables;
        private Coroutine _typeRoutine;
        private Coroutine _waitChromeRoutine;
        private Coroutine _endingRoutine;
        private string _pendingFeed;
        private CipherMessageData _boundMessage;
        private bool _showFullTextOnNextOpen;

        public Sprite TaskbarIcon => _taskbarIcon;
        public Sprite TaskbarHighlightSprite => _taskbarHighlightSprite;
        public Sprite TaskbarPressedSprite => _taskbarPressedSprite;

        [Inject]
        private void Construct(
            [InjectOptional] IGameFlowPresentationModel presentation,
            [InjectOptional] IEndingCipherMessages endings,
            [InjectOptional] GameExitFlowController exitFlow)
        {
            _presentation = presentation;
            _endings = endings;
            _exitFlow = exitFlow;
        }

        private void Awake()
        {
            _output.richText = false;
            if (_scrollRect != null)
                ConfigureScrollRect();
        }

        private void Start()
        {
            ResolvePresentationIfNeeded();
            if (_presentation == null)
            {
                Debug.LogWarning($"{nameof(TerminalView)}: no {nameof(IGameFlowPresentationModel)} (Zenject or scene).", this);
                return;
            }

            _disposables = new CompositeDisposable();
            _presentation.CurrentTerminalMessage.Subscribe(Display).AddTo(_disposables);
        }

        private void OnEnable()
        {
            TerminalNotificationIndicator.NotifyTerminalOpened();

            ResolvePresentationIfNeeded();
            if (_presentation == null)
                return;

            if (_disposables != null)
                Display(_presentation.CurrentTerminalMessage.Value);
        }

        private void ResolvePresentationIfNeeded()
        {
            if (_presentation != null)
                return;

            var controller = Object.FindAnyObjectByType<GameFlowController>();
            if (controller != null)
            {
                _presentation = controller.Presentation;
                _endings ??= controller;
            }
        }

        public void BindDesktopServices(DesktopWindow desktopWindow, DesktopClockWidget cornerClockWidget)
        {
            _desktopWindow = desktopWindow;
            _clockWidget = cornerClockWidget;
        }

        private void OnDestroy()
        {
            StopDeferredChromeWait();
            _disposables?.Dispose();
        }

        private void OnDisable()
        {
            if (_boundMessage != null)
                _showFullTextOnNextOpen = true;

            StopAlienAudio();
            StopDeferredChromeWait();
            StopTypewriter();
            StopEndingRoutine();
        }

        public void Display(CipherMessageData message)
        {
            StopDeferredChromeWait();
            StopTypewriter();
            StopEndingRoutine();
            StopAlienAudio();

            if (message == null)
            {
                _boundMessage = null;
                _showFullTextOnNextOpen = false;
                _pendingFeed = null;
                _output.text = string.Empty;
                UpdateScrollContentHeight();
                return;
            }

            var isNewCipher = _boundMessage == null || !ReferenceEquals(_boundMessage, message);
            if (isNewCipher)
            {
                _boundMessage = message;
                _showFullTextOnNextOpen = false;
            }

            if (!isNewCipher && _showFullTextOnNextOpen)
            {
                ShowFullMessageText(message);
                return;
            }

            var playback = _presentation != null
                ? _presentation.TerminalEndingPlayback.Value
                : TerminalEndingPlayback.None;

            if (playback == TerminalEndingPlayback.Bad && _endings?.BadEnding != null)
            {
                _pendingFeed = message.TerminalFeedText;
                _endingRoutine = StartCoroutine(WaitUntilTerminalVisibleThenBadEnding(message));
                return;
            }

            if (playback == TerminalEndingPlayback.Good && _endings?.GoodEnding != null)
            {
                _pendingFeed = message.TerminalFeedText;
                _endingRoutine = StartCoroutine(WaitUntilTerminalVisibleThenGoodEnding(message));
                return;
            }

            if (playback == TerminalEndingPlayback.GoodCredits && _endings?.GoodEndingCredits != null)
            {
                _pendingFeed = message.TerminalFeedText;
                if (ShouldDeferPlaybackUntilVisible())
                    _waitChromeRoutine = StartCoroutine(WaitUntilVisibleThenGoodCredits(message));
                else
                {
                    TryPlayAlienAudio(message);
                    StartTypewriter(_pendingFeed);
                }

                return;
            }

            _pendingFeed = message.TerminalFeedText;
            if (ShouldDeferPlaybackUntilVisible())
            {
                _waitChromeRoutine = StartCoroutine(WaitUntilVisibleThenPlay());
                return;
            }

            TryPlayAlienAudio(message);
            StartTypewriter(_pendingFeed);
        }

        private void StopEndingRoutine()
        {
            if (_endingRoutine == null)
                return;

            StopCoroutine(_endingRoutine);
            _endingRoutine = null;
        }

        private IEnumerator BadEndingSequence(CipherMessageData message)
        {
            TryPlayAlienAudio(message);

            if (_presentation != null)
            {
                _presentation.IsDecoderSubmissionEnabled.Value = false;
                _presentation.BlockAllPlayerInput.Value = false;
            }

            SetTerminalCommandsLocked(true);

            var feed = message.TerminalFeedText;
            if (string.IsNullOrWhiteSpace(feed))
                feed = "…";

            yield return Typewriter(feed);

            yield return GlitchFlickerRoutine();

            _output.text = DefaultBadEndingCipherBlock;
            RefreshLayoutAndMaybePinToBottom();

            if (_presentation != null)
                _presentation.BlockAllPlayerInput.Value = true;

            var reveal = message.EndingTranslationRevealText ?? string.Empty;
            var pending = 2;
            IEnumerator RunAndSignal(IEnumerator inner)
            {
                yield return StartCoroutine(inner);
                pending--;
            }

            StartCoroutine(RunAndSignal(RevealPlaintextDigitToLetter(reveal)));
            StartCoroutine(RunAndSignal(ClockRampOrWait()));
            while (pending > 0)
                yield return null;

            yield return new WaitForSeconds(_badEndingPostDelaySeconds);

            SetTerminalCommandsLocked(false);

            if (_presentation != null)
                _presentation.BlockAllPlayerInput.Value = false;

            if (_exitFlow != null)
                _exitFlow.ShowEndGame(EndingKind.Bad);

            _endingRoutine = null;
        }

        private IEnumerator ClockRampOrWait()
        {
            if (_clockWidget != null)
                yield return _clockWidget.AnimateToNinetyNine(_badEndingClockRampSeconds);
            else
                yield return new WaitForSeconds(_badEndingClockRampSeconds);
        }

        private IEnumerator WaitUntilTerminalVisibleThenBadEnding(CipherMessageData message)
        {
            while (ShouldDeferPlaybackUntilVisible())
                yield return null;

            yield return BadEndingSequence(message);
        }

        private IEnumerator WaitUntilTerminalVisibleThenGoodEnding(CipherMessageData message)
        {
            while (ShouldDeferPlaybackUntilVisible())
                yield return null;

            yield return GoodEndingSequence(message);
        }

        private IEnumerator WaitUntilVisibleThenGoodCredits(CipherMessageData message)
        {
            while (ShouldDeferPlaybackUntilVisible())
                yield return null;

            _waitChromeRoutine = null;
            TryPlayAlienAudio(message);
            StartTypewriter(_pendingFeed ?? string.Empty);
        }

        private IEnumerator GoodEndingBodyLineByLine(string fullText)
        {
            _output.text = string.Empty;
            RefreshLayoutAndMaybePinToBottom();
            var lines = fullText.Split('\n');
            var buffer = new StringBuilder(256);
            var charDelay = new WaitForSeconds(_secondsPerCharacter);
            var betweenLines = new WaitForSeconds(_goodEndingLinePauseSeconds);

            for (var i = 0; i < lines.Length; i++)
            {
                foreach (var ch in lines[i])
                {
                    buffer.Append(ch);
                    _output.text = buffer.ToString();
                    RefreshLayoutAndMaybePinToBottom();
                    yield return charDelay;
                }

                if (i >= lines.Length - 1)
                    continue;

                buffer.Append('\n');
                _output.text = buffer.ToString();
                RefreshLayoutAndMaybePinToBottom();
                yield return betweenLines;
            }

            RefreshLayoutAndMaybePinToBottom();
        }

        private IEnumerator GoodEndingSequence(CipherMessageData message)
        {
            TryPlayAlienAudio(message);

            if (_presentation != null)
            {
                _presentation.IsDecoderSubmissionEnabled.Value = false;
                _presentation.BlockAllPlayerInput.Value = false;
            }

            SetTerminalCommandsLocked(true);

            yield return GoodEndingBodyLineByLine(message.TerminalFeedText ?? string.Empty);

            SetTerminalCommandsLocked(false);

            if (_presentation != null)
            {
                _presentation.IsDecoderSubmissionEnabled.Value = true;
                _presentation.BlockAllPlayerInput.Value = false;
            }

            yield return new WaitForSeconds(_goodEndingCreditsDelaySeconds);

            if (_endings != null && _endings.GoodEndingCredits != null && _presentation != null)
            {
                _presentation.TerminalEndingPlayback.Value = TerminalEndingPlayback.GoodCredits;
                _presentation.CurrentTerminalMessage.Value = _endings.GoodEndingCredits;
            }

            _endingRoutine = null;
        }

        private IEnumerator GlitchFlickerRoutine()
        {
            var original = _output.text;
            for (var i = 0; i < 10; i++)
            {
                _output.text = (i % 2 == 0) ? string.Empty : RandomNoiseText(original.Length);
                RefreshLayoutAndMaybePinToBottom();
                yield return null;
            }

            _output.text = original;
            RefreshLayoutAndMaybePinToBottom();
        }

        private static string RandomNoiseText(int length)
        {
            const string chars = "01█▓▒░";
            var sb = new StringBuilder(length);
            for (var i = 0; i < length; i++)
                sb.Append(chars[Random.Range(0, chars.Length)]);

            return sb.ToString();
        }

        private IEnumerator RevealPlaintextDigitToLetter(string target)
        {
            if (string.IsNullOrEmpty(target))
                yield break;

            var current = new char[target.Length];
            var letterIndices = new List<int>();
            for (var i = 0; i < target.Length; i++)
            {
                var c = target[i];
                if (char.IsLetter(c))
                {
                    current[i] = (char)('0' + Random.Range(0, 10));
                    letterIndices.Add(i);
                }
                else
                {
                    current[i] = c;
                }
            }

            Shuffle(letterIndices);

            _output.text = new string(current);
            RefreshLayoutAndMaybePinToBottom();

            var step = new WaitForSeconds(0.04f);
            foreach (var idx in letterIndices)
            {
                current[idx] = target[idx];
                _output.text = new string(current);
                RefreshLayoutAndMaybePinToBottom();
                yield return step;
            }
        }

        private static void Shuffle(IList<int> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void SetTerminalCommandsLocked(bool locked) =>
            _desktopWindow?.SetAppWindowCommandsInteractable(DesktopAppKind.Terminal, !locked);

        private void StopAlienAudio() => _alienAudioSource?.Stop();

        private void TryPlayAlienAudio(CipherMessageData message)
        {
            if (_alienAudioSource == null || _alienAudioClip == null || message == null || !message.IsAlienMessage)
                return;

            _alienAudioSource.clip = _alienAudioClip;
            _alienAudioSource.Play();
        }

        private void ShowFullMessageText(CipherMessageData message)
        {
            _pendingFeed = message.TerminalFeedText;
            _output.text = _pendingFeed ?? string.Empty;
            UpdateScrollContentHeight();
            RefreshLayoutAndMaybePinToBottom();
            TryPlayAlienAudio(message);
        }

        private void StopDeferredChromeWait()
        {
            if (_waitChromeRoutine == null)
                return;

            StopCoroutine(_waitChromeRoutine);
            _waitChromeRoutine = null;
        }

        private IEnumerator WaitUntilVisibleThenPlay()
        {
            while (ShouldDeferPlaybackUntilVisible())
                yield return null;

            _waitChromeRoutine = null;
            if (string.IsNullOrEmpty(_pendingFeed))
                yield break;

            if (_showFullTextOnNextOpen && _boundMessage != null)
            {
                ShowFullMessageText(_boundMessage);
                yield break;
            }

            TryPlayAlienAudio(_boundMessage);
            StartTypewriter(_pendingFeed);
        }

        private bool ShouldDeferPlaybackUntilVisible()
        {
            if (_desktopWindow != null)
                return !_desktopWindow.IsWindowChromeActiveInHierarchy(DesktopAppKind.Terminal);

            return !gameObject.activeInHierarchy;
        }

        private void ConfigureScrollRect()
        {
            EnsureViewportFillsScrollAreaIfCollapsed();
            _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            _scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            _scrollRect.scrollSensitivity = _scrollSensitivity;
            EnsureViewportReceivesPointerEvents();
        }

        private void EnsureViewportFillsScrollAreaIfCollapsed()
        {
            var vp = _scrollRect.viewport;
            if (vp == null)
                return;

            if (vp.rect.width > 2f && vp.rect.height > 2f)
                return;

            vp.anchorMin = Vector2.zero;
            vp.anchorMax = Vector2.one;
            vp.pivot = new Vector2(0.5f, 0.5f);
            vp.offsetMin = Vector2.zero;
            vp.offsetMax = Vector2.zero;
            vp.anchoredPosition3D = Vector3.zero;
            vp.localScale = Vector3.one;
        }

        private void EnsureViewportReceivesPointerEvents()
        {
            var viewport = _scrollRect.viewport;
            if (viewport == null || viewport.GetComponent<Graphic>() != null)
                return;

            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
        }

        private void UpdateScrollContentHeight()
        {
            if (_scrollRect == null || _output == null)
                return;

            var content = _scrollRect.content;
            if (content == null)
                return;

            _output.ForceMeshUpdate();

            const float padding = 8f;
            var textHeight = _output.preferredHeight + padding;
            var viewport = _scrollRect.viewport;
            var viewHeight = viewport != null ? viewport.rect.height : 0f;
            var targetHeight = viewHeight > 1f ? Mathf.Max(textHeight, viewHeight) : textHeight;

            if (_output.rectTransform == content)
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            else
            {
                _output.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                var layoutHeight = LayoutUtility.GetPreferredHeight(content);
                if (layoutHeight > 0.01f)
                    content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(layoutHeight, viewHeight > 1f ? viewHeight : layoutHeight));
                else
                    content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            }

            Canvas.ForceUpdateCanvases();
        }

        private bool ContentHeightExceedsViewport()
        {
            if (_scrollRect?.content == null || _scrollRect.viewport == null)
                return false;

            return _scrollRect.content.rect.height > _scrollRect.viewport.rect.height + _overflowPixels;
        }

        private void RefreshLayoutAndMaybePinToBottom()
        {
            if (_scrollRect == null)
                return;

            UpdateScrollContentHeight();
            if (!ContentHeightExceedsViewport())
                return;

            _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void StartTypewriter(string fullText) => _typeRoutine = StartCoroutine(Typewriter(fullText));

        private void StopTypewriter()
        {
            if (_typeRoutine == null)
                return;

            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }

        private IEnumerator Typewriter(string fullText)
        {
            _output.text = string.Empty;
            RefreshLayoutAndMaybePinToBottom();
            var buffer = new StringBuilder(256);
            var lines = fullText.Split('\n');
            var charDelay = new WaitForSeconds(_secondsPerCharacter);
            var lineDelay = new WaitForSeconds(_pauseAfterLine);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                foreach (var ch in line)
                {
                    buffer.Append(ch);
                    _output.text = buffer.ToString();
                    RefreshLayoutAndMaybePinToBottom();
                    yield return charDelay;
                }

                if (i < lines.Length - 1)
                {
                    buffer.Append('\n');
                    _output.text = buffer.ToString();
                    RefreshLayoutAndMaybePinToBottom();
                    yield return lineDelay;
                }
            }

            RefreshLayoutAndMaybePinToBottom();
            _typeRoutine = null;
        }
    }
}
