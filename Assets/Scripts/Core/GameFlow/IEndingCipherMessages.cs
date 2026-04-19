namespace LudumDare2026.Core.GameFlow
{
    public interface IEndingCipherMessages
    {
        CipherMessageData BadEnding { get; }

        CipherMessageData GoodEnding { get; }

        CipherMessageData GoodEndingCredits { get; }
    }
}
