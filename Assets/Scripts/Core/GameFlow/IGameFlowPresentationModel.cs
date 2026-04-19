using System;
using UniRx;

namespace LudumDare2026.Core.GameFlow
{
    public interface IGameFlowPresentationModel
    {
        IReactiveProperty<CipherMessageData> CurrentTerminalMessage { get; }

        IReactiveProperty<string> Status { get; }

        IObservable<string> DecoderAnswers { get; }

        IReactiveProperty<string> ClockDisplayText { get; }

        IReactiveProperty<bool> IsDecoderSubmissionEnabled { get; }

        IReactiveProperty<bool> BlockAllPlayerInput { get; }

        IReactiveProperty<bool> SuppressQuitEndingStoryText { get; }

        IReactiveProperty<TerminalEndingPlayback> TerminalEndingPlayback { get; }

        void PublishDecoderAnswer(string answer);
    }
}
