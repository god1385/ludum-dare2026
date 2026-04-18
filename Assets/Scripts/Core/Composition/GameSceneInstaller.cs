using LudumDare2026.Core.Cipher;
using LudumDare2026.Core.Desktop;
using LudumDare2026.Core.GameFlow;
using LudumDare2026.Core.Shop;
using LudumDare2026.Core.Windows;
using UnityEngine;
using Zenject;

namespace LudumDare2026.Core.Composition
{
    public class GameSceneInstaller : MonoInstaller
    {
        [SerializeField] private GameContentConfig _content;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<GameFlowPresentationModel>().AsSingle();
            Container.BindInstance(_content);
            Container.Bind<ICipherAnswerValidator>().To<ExactCipherAnswerValidator>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerWallet>().AsSingle();
            Container.Bind<PlayerCursorInventory>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameFlowController>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesAndSelfTo<ReactiveGameHudBinder>().FromComponentInHierarchy().AsSingle();
            Container.Bind<GameCursorController>().FromComponentInHierarchy().AsSingle();
        }
    }
}
