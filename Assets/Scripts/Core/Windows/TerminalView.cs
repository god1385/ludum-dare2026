using System.Collections;
using System.Text;
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
        [SerializeField] private TextMeshProUGUI _output;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private float _secondsPerCharacter = 0.025f;
        [SerializeField] private float _pauseAfterLine = 0.12f;
        [SerializeField] private float _overflowPixels = 2f;
        [SerializeField] private float _scrollSensitivity = 35f;
        [SerializeField] private Sprite _taskbarIcon;
        [SerializeField] private Sprite _taskbarHighlightSprite;
        [SerializeField] private Sprite _taskbarPressedSprite;

        private IGameFlowPresentationModel _presentation;
        private CompositeDisposable _disposables;
        private Coroutine _typeRoutine;
        private Coroutine _waitChromeRoutine;
        private string _pendingFeed;
        private CipherMessageData _boundMessage;
        private bool _showFullTextOnNextOpen;

        public Sprite TaskbarIcon => _taskbarIcon;
        public Sprite TaskbarHighlightSprite => _taskbarHighlightSprite;
        public Sprite TaskbarPressedSprite => _taskbarPressedSprite;

        [Inject]
        private void Construct([InjectOptional] IGameFlowPresentationModel presentation) => _presentation = presentation;

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
                _presentation = controller.Presentation;
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

            StopDeferredChromeWait();
            StopTypewriter();
        }

        public void Display(CipherMessageData message)
        {
            StopDeferredChromeWait();
            StopTypewriter();
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

            _pendingFeed = message.TerminalFeedText;
            if (ShouldDeferPlaybackUntilVisible())
            {
                _waitChromeRoutine = StartCoroutine(WaitUntilVisibleThenPlay());
                return;
            }

            StartTypewriter(_pendingFeed);
        }

        private void ShowFullMessageText(CipherMessageData message)
        {
            _pendingFeed = message.TerminalFeedText;
            _output.text = _pendingFeed ?? string.Empty;
            UpdateScrollContentHeight();
            RefreshLayoutAndMaybePinToBottom();
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

            StartTypewriter(_pendingFeed);
        }

        /// <summary>
        /// Coroutines do not run on inactive GameObjects; <see cref="DesktopWindowChrome"/> may live on an
        /// always-active parent while this view is under the toggled layout root, so we key off self visibility.
        /// </summary>
        private bool ShouldDeferPlaybackUntilVisible() => !gameObject.activeInHierarchy;

        private void ConfigureScrollRect()
        {
            EnsureViewportFillsScrollAreaIfCollapsed();
            _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            _scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            _scrollRect.scrollSensitivity = _scrollSensitivity;
            EnsureViewportReceivesPointerEvents();
        }

        /// <summary>
        /// If the viewport lost size (0×0 etc.), stretch it to the ScrollRect parent so content is visible.
        /// </summary>
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

        /// <summary>
        /// TMP grows text visually but the ScrollRect <see cref="ScrollRect.content"/> height must follow
        /// <see cref="TextMeshProUGUI.preferredHeight"/> or scrolling has no range.
        /// </summary>
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
