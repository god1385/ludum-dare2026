using UnityEngine;

namespace LudumDare2026.Core.GameFlow
{
    [CreateAssetMenu(fileName = "CipherMessage", menuName = "Game/Cipher Message", order = 0)]
    public class CipherMessageData : ScriptableObject
    {
        [SerializeField] private string _title;
        [SerializeField] [TextArea(2, 6)] private string _encryptedBody;
        [SerializeField] private string[] _tags = { "cipher", "urgent" };
        [SerializeField] private string _expectedAnswer;
        [SerializeField] [TextArea(10, 32)] private string _terminalFeed;

        [SerializeField] private string _clockTimeText = "12:00";
        [SerializeField] private bool _isEndingMessage;
        [SerializeField] [TextArea(10, 32)] private string _endingTranslationRevealText;
        [SerializeField] private bool _isAlienMessage;

        public string Title => _title;
        public string EncryptedBody => _encryptedBody;
        public string[] Tags => _tags;
        public string ExpectedAnswer => _expectedAnswer;

        public string ClockTimeText => _clockTimeText ?? string.Empty;

        public bool IsEndingMessage => _isEndingMessage;

        public string EndingTranslationRevealText => _endingTranslationRevealText ?? string.Empty;

        public bool IsAlienMessage => _isAlienMessage;

        public string TerminalFeedText =>
            string.IsNullOrEmpty(_terminalFeed) ? (_encryptedBody ?? string.Empty) : _terminalFeed;
    }
}
