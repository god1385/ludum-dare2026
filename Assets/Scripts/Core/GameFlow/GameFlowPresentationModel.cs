using System;
using UniRx;

namespace LudumDare2026.Core.GameFlow
{
    public class GameFlowPresentationModel : IGameFlowPresentationModel
    {
        private readonly Subject<string> _decoderAnswers = new Subject<string>();

        public IReactiveProperty<CipherMessageData> CurrentTerminalMessage { get; } = new ReactiveProperty<CipherMessageData>();

        public IReactiveProperty<string> Status { get; } = new ReactiveProperty<string>();

        public IReactiveProperty<string> ClockDisplayText { get; } = new ReactiveProperty<string>(string.Empty);

        public IReactiveProperty<bool> IsDecoderSubmissionEnabled { get; } = new ReactiveProperty<bool>(true);

        public IReactiveProperty<bool> BlockAllPlayerInput { get; } = new ReactiveProperty<bool>(false);

        public IReactiveProperty<bool> SuppressQuitEndingStoryText { get; } = new ReactiveProperty<bool>(false);

        public IReactiveProperty<TerminalEndingPlayback> TerminalEndingPlayback { get; } =
            new ReactiveProperty<TerminalEndingPlayback>(default);

        public IObservable<string> DecoderAnswers => _decoderAnswers;

        public void PublishDecoderAnswer(string answer) => _decoderAnswers.OnNext(answer ?? string.Empty);
    }
}
