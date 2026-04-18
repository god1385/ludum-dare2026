using System;
using UniRx;

namespace LudumDare2026.Core.GameFlow
{
    public sealed class GameFlowPresentationModel : IGameFlowPresentationModel
    {
        private readonly Subject<string> _decoderAnswers = new Subject<string>();

        public IReactiveProperty<CipherMessageData> CurrentTerminalMessage { get; } = new ReactiveProperty<CipherMessageData>();

        public IReactiveProperty<string> Status { get; } = new ReactiveProperty<string>();

        public IObservable<string> DecoderAnswers => _decoderAnswers;

        public void PublishDecoderAnswer(string answer) => _decoderAnswers.OnNext(answer ?? string.Empty);
    }
}
