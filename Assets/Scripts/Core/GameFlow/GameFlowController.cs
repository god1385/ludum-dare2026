using LudumDare2026.Core.Cipher;
using UniRx;
using UnityEngine;
using Zenject;

namespace LudumDare2026.Core.GameFlow
{
    public class GameFlowController : MonoBehaviour
    {
        private IGameFlowPresentationModel _presentation;
        private ICipherAnswerValidator _validator;
        private GameContentConfig _content;
        private IPlayerWallet _wallet;
        private int _messageIndex;

        public IGameFlowPresentationModel Presentation => _presentation;

        [Inject]
        private void Construct(
            IGameFlowPresentationModel presentation,
            ICipherAnswerValidator validator,
            GameContentConfig content,
            IPlayerWallet wallet)
        {
            _presentation = presentation;
            _validator = validator;
            _content = content;
            _wallet = wallet;
        }

        private void Start()
        {
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
            _presentation.CurrentTerminalMessage.Value = _content.Messages[_messageIndex];
            _presentation.Status.Value = $"Message {_messageIndex + 1} / {_content.MessageCount}";
        }

        private void OnDecoderAnswerSubmitted(string answer)
        {
            if (_content.MessageCount == 0)
                return;

            var message = _content.Messages[_messageIndex];
            if (!_validator.IsCorrect(message, answer))
            {
                _presentation.Status.Value = "Wrong answer. Try again.";
                return;
            }

            _wallet.Add(1);
            _messageIndex++;
            if (_messageIndex >= _content.MessageCount)
            {
                _presentation.CurrentTerminalMessage.Value = null;
                _presentation.Status.Value = "All messages cleared. Good work.";
                return;
            }

            PresentCurrentMessage();
        }
    }
}
