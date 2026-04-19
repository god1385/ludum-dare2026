using UnityEngine;

namespace LudumDare2026.Core.GameFlow
{
    [CreateAssetMenu(menuName = "LudumDare2026/Game Flow/Ending Texts", fileName = "EndingTexts")]
    public class EndingTextsConfig : ScriptableObject
    {
        [SerializeField] private string _badEndingText;
        [SerializeField] private string _neutralEndingText;
        [SerializeField] private string _goodEndingText;

        public string GetText(EndingKind kind) =>
            kind switch
            {
                EndingKind.Bad => _badEndingText ?? string.Empty,
                EndingKind.Neutral => _neutralEndingText ?? string.Empty,
                EndingKind.Good => _goodEndingText ?? string.Empty,
                _ => string.Empty,
            };
    }
}
