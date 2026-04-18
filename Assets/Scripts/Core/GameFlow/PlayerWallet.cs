using UniRx;

namespace LudumDare2026.Core.GameFlow
{
    public sealed class PlayerWallet : IPlayerWallet
    {
        private readonly ReactiveProperty<int> _dollars = new ReactiveProperty<int>(0);

        public IReadOnlyReactiveProperty<int> Dollars => _dollars;

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            _dollars.Value += amount;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || _dollars.Value < amount)
                return false;

            _dollars.Value -= amount;
            return true;
        }
    }
}
