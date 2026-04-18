using LudumDare2026.Core.GameFlow;
using LudumDare2026.Utilities;

namespace LudumDare2026.Core.Cipher
{
    public class ExactCipherAnswerValidator : ICipherAnswerValidator
    {
        public bool IsCorrect(CipherMessageData message, string userAnswer) =>
            CipherAnswerNormalizer.Normalize(userAnswer) == CipherAnswerNormalizer.Normalize(message.ExpectedAnswer);
    }
}
