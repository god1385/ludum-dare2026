using TMPro;
using UnityEngine;

namespace LudumDare2026.Core.Windows
{
    /// <summary>Player-authored notes only; not driven by game flow.</summary>
    public class NotebookView : MonoBehaviour, IWindowTaskbarIconSource
    {
        [SerializeField] private TMP_InputField _notesField;
        [SerializeField] private AudioClip _keyboardTypingLoop;
        [Tooltip("Optional: dedicated AudioSource for long typing loop.")]
        [SerializeField] private AudioSource _keyboardTypingAudioSource;
        [SerializeField] private Sprite _taskbarIcon;
        [SerializeField] private Sprite _taskbarHighlightSprite;
        [SerializeField] private Sprite _taskbarPressedSprite;

        public Sprite TaskbarIcon => _taskbarIcon;
        public Sprite TaskbarHighlightSprite => _taskbarHighlightSprite;
        public Sprite TaskbarPressedSprite => _taskbarPressedSprite;

        public TMP_InputField NotesField => _notesField;

        private void Awake()
        {
            ConfigureNotesField();
            KeyboardTypingLoopAudio.Ensure(_notesField, _keyboardTypingLoop, 0.5f, _keyboardTypingAudioSource);
        }

        private void ConfigureNotesField()
        {
            if (_notesField == null)
                return;

            // Enter inserts newline; does not deactivate / fire submit (unlike SingleLine or MultiLineSubmit).
            _notesField.lineType = TMP_InputField.LineType.MultiLineNewline;
        }
    }
}
