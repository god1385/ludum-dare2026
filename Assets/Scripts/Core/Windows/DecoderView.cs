using LudumDare2026.Core.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LudumDare2026.Core.Windows
{
    public class DecoderView : MonoBehaviour, IWindowTaskbarIconSource
    {
        [SerializeField] private TMP_InputField _answerField;
        [SerializeField] private AudioClip _keyboardTypingLoop;
        [Tooltip("Optional: dedicated AudioSource for long typing loop (e.g. second channel next to click SFX).")]
        [SerializeField] private AudioSource _keyboardTypingAudioSource;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Sprite _taskbarIcon;
        [SerializeField] private Sprite _taskbarHighlightSprite;
        [SerializeField] private Sprite _taskbarPressedSprite;

        private IGameFlowPresentationModel _presentation;

        [Inject]
        private void Construct(IGameFlowPresentationModel presentation) => _presentation = presentation;

        public Sprite TaskbarIcon => _taskbarIcon;
        public Sprite TaskbarHighlightSprite => _taskbarHighlightSprite;
        public Sprite TaskbarPressedSprite => _taskbarPressedSprite;

        private void Awake()
        {
            _submitButton.onClick.AddListener(SubmitAnswer);
            KeyboardTypingLoopAudio.Ensure(_answerField, _keyboardTypingLoop, 0.5f, _keyboardTypingAudioSource);
        }

        private void SubmitAnswer()
        {
            if (_presentation != null)
                _presentation.PublishDecoderAnswer(_answerField.text);
        }
    }
}
