using System;
using UniRx;

namespace LudumDare2026.Core.GameFlow
{
    public interface IGameFlowPresentationModel
    {
        IReactiveProperty<CipherMessageData> CurrentTerminalMessage { get; }

        IReactiveProperty<string> Status { get; }

        IObservable<string> DecoderAnswers { get; }

        void PublishDecoderAnswer(string answer);
    }
}
