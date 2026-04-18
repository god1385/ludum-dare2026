using System.Collections.Generic;
using UniRx;

namespace LudumDare2026.Core.Shop
{
    /// <summary>
    /// Tracks owned cursor skins and which one is equipped (null = use default sprites on <see cref="Desktop.GameCursorController"/>).
    /// </summary>
    public sealed class PlayerCursorInventory
    {
        private readonly HashSet<ShopCursorItemDefinition> _owned = new HashSet<ShopCursorItemDefinition>();
        private readonly ReactiveProperty<ShopCursorItemDefinition> _equipped = new ReactiveProperty<ShopCursorItemDefinition>(null);

        public IReadOnlyReactiveProperty<ShopCursorItemDefinition> Equipped => _equipped;

        public bool IsOwned(ShopCursorItemDefinition definition) =>
            definition != null && _owned.Contains(definition);

        public void MarkOwned(ShopCursorItemDefinition definition)
        {
            if (definition == null)
                return;

            _owned.Add(definition);
        }

        /// <summary>Pass null to use built-in default cursor sprites from the controller.</summary>
        public bool TryEquip(ShopCursorItemDefinition definition)
        {
            if (definition != null && !_owned.Contains(definition))
                return false;

            _equipped.Value = definition;
            return true;
        }
    }
}
