using UniRx;

namespace LudumDare2026.Core.GameFlow
{
    public interface IPlayerWallet
    {
        IReadOnlyReactiveProperty<int> Dollars { get; }

        void Add(int amount);

        bool TrySpend(int amount);
    }
}
