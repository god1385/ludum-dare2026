using LudumDare2026.Core.GameFlow;

namespace LudumDare2026.Core.Cipher
{
    public interface ICipherAnswerValidator
    {
        bool IsCorrect(CipherMessageData message, string userAnswer);
    }
}
