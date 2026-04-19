using LudumDare2026.Core.Cipher;
using LudumDare2026.Core.Shop;
using UniRx;
using UnityEngine;
using Zenject;

namespace LudumDare2026.Core.GameFlow
{
    public class GameFlowController : MonoBehaviour, IEndingCipherMessages
    {
        [SerializeField] private CipherMessageData _badEndingMessage;
        [SerializeField] private CipherMessageData _goodEndingMessage;
        [SerializeField] private CipherMessageData _goodEndingCreditsMessage;
        [SerializeField] private string _goodEndingCursorId = string.Empty;
        [SerializeField] private AudioSource _backgroundMusicSource;
        [SerializeField] private AudioClip _badEndingMusic;
        [SerializeField] private AudioClip _goodEndingMusic;
        [SerializeField] private SplashScreenController _splashScreen;

        private IGameFlowPresentationModel _presentation;
        private ICipherAnswerValidator _validator;
        private GameContentConfig _content;
        private IPlayerWallet _wallet;
        private PlayerCursorInventory _inventory;

        private int _messageIndex;

        private bool _gameplayBegun;

        public IGameFlowPresentationModel Presentation => _presentation;

        public CipherMessageData BadEnding => _badEndingMessage;

        public CipherMessageData GoodEnding => _goodEndingMessage;

        public CipherMessageData GoodEndingCredits => _goodEndingCreditsMessage;

        [Inject]
        private void Construct(
            IGameFlowPresentationModel presentation,
            ICipherAnswerValidator validator,
            GameContentConfig content,
            IPlayerWallet wallet,
            PlayerCursorInventory inventory)
        {
            _presentation = presentation;
            _validator = validator;
            _content = content;
            _wallet = wallet;
            _inventory = inventory;
        }

        private void Start()
        {
            if (_splashScreen != null)
            {
                // Same canvas as desktop: BlockAllPlayerInput would turn off the whole CanvasGroup (splash included).
                _splashScreen.Dismissed += OnSplashDismissed;
                return;
            }

            BeginGameplay();
        }

        private void OnDestroy()
        {
            if (_splashScreen != null)
                _splashScreen.Dismissed -= OnSplashDismissed;
        }

        private void OnSplashDismissed() => BeginGameplay();

        /// <summary>Starts the cipher loop after the optional splash screen. Idempotent.</summary>
        public void BeginGameplay()
        {
            if (_gameplayBegun)
                return;

            _gameplayBegun = true;
            _presentation.BlockAllPlayerInput.Value = false;

            if (_content == null || _content.MessageCount == 0)
            {
                _presentation.Status.Value = _content == null
                    ? "Game content config is not assigned."
                    : "No cipher messages configured.";
                return;
            }

            _messageIndex = 0;
            PresentCurrentMessage();
            _presentation.DecoderAnswers.Subscribe(OnDecoderAnswerSubmitted).AddTo(this);
        }

        private void PresentCurrentMessage()
        {
            _presentation.SuppressQuitEndingStoryText.Value = false;
            _presentation.TerminalEndingPlayback.Value = TerminalEndingPlayback.None;
            _presentation.CurrentTerminalMessage.Value = _content.Messages[_messageIndex];
            ApplyClockForMessage(_content.Messages[_messageIndex]);
            _presentation.Status.Value = $"Message {_messageIndex + 1} / {_content.MessageCount}";
        }

        private void ApplyClockForMessage(CipherMessageData message)
        {
            if (message == null)
                return;

            _presentation.ClockDisplayText.Value = message.ClockTimeText;
        }

        private void OnDecoderAnswerSubmitted(string answer)
        {
            if (_content.MessageCount == 0)
                return;

            if (!_presentation.IsDecoderSubmissionEnabled.Value)
            {
                _presentation.Status.Value = "…";
                return;
            }

            var message = _content.Messages[_messageIndex];
            if (!_validator.IsCorrect(message, answer))
            {
                _presentation.Status.Value = "Wrong answer. Try again.";
                return;
            }

            _wallet.Add(1);

            if (message.IsEndingMessage)
            {
                BeginEndingBranch();
                return;
            }

            _messageIndex++;
            if (_messageIndex >= _content.MessageCount)
            {
                _presentation.TerminalEndingPlayback.Value = TerminalEndingPlayback.None;
                _presentation.CurrentTerminalMessage.Value = null;
                _presentation.Status.Value = "All messages cleared. Good work.";
                return;
            }

            PresentCurrentMessage();
        }

        private void BeginEndingBranch()
        {
            _messageIndex++;
            var good = _inventory.OwnsCursorWithId(_goodEndingCursorId);
            if (good)
            {
                if (_goodEndingMessage == null)
                {
                    _presentation.Status.Value = "Good ending message is not assigned.";
                    return;
                }

                _presentation.SuppressQuitEndingStoryText.Value = true;
                _presentation.TerminalEndingPlayback.Value = TerminalEndingPlayback.Good;
                _presentation.CurrentTerminalMessage.Value = _goodEndingMessage;
                ApplyClockForMessage(_goodEndingMessage);
                PlayEndingMusic(true);
                _presentation.Status.Value = "…";
                return;
            }

            if (_badEndingMessage == null)
            {
                _presentation.Status.Value = "Bad ending message is not assigned.";
                return;
            }

            _presentation.SuppressQuitEndingStoryText.Value = false;
            _presentation.TerminalEndingPlayback.Value = TerminalEndingPlayback.Bad;
            _presentation.CurrentTerminalMessage.Value = _badEndingMessage;
            ApplyClockForMessage(_badEndingMessage);
            PlayEndingMusic(false);
            _presentation.Status.Value = "…";
        }

        private void PlayEndingMusic(bool goodEnding)
        {
            if (_backgroundMusicSource == null)
                return;

            var clip = goodEnding ? _goodEndingMusic : _badEndingMusic;
            if (clip == null)
                return;

            _backgroundMusicSource.clip = clip;
            _backgroundMusicSource.Play();
        }
    }
}
