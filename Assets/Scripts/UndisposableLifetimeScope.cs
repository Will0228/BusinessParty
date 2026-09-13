using MixVerse.Game;
using MixVerse.Game.Model;
using MixVerse.Game.Player;
using MixVerse.Home;
using MixVerse.Midi;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MixVerse
{
    public sealed class UndisposableLifetimeScope : LifetimeScope
    {
        [SerializeField] private HomeView _homeView;
        [SerializeField] private GameView _gameView;
        [SerializeField] private DjControllerInput _djControllerInput;
        
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            
            // 画面の切り替えは ScreenNavigator が受け持つので、起点もここになる
            builder.RegisterEntryPoint<ScreenNavigator>().AsSelf();

            builder.Register<TweenUtility>(Lifetime.Singleton);
            
            builder.Register<HomeController>(Lifetime.Singleton);
            builder.RegisterComponent(_homeView);
            builder.Register<HomePresenter>(Lifetime.Singleton).As<IHomePresenter>();
            
            builder.Register<GameController>(Lifetime.Singleton);
            builder.RegisterComponent(_gameView);
            builder.Register<GamePresenter>(Lifetime.Singleton).As<IGamePresenter>();
            builder.RegisterInstance(_gameView.Settings);
            builder.RegisterComponent(_gameView.PlayerView);
            builder.Register<PlayerPresenter>(Lifetime.Singleton).As<IPlayerPresenter>();
            builder.Register<CpuTalkScript>(Lifetime.Singleton);
            
            RegisterDjController(builder);
        }
        
        /// <summary>
        /// DJ コントローラーは任意接続なので、未設定でもコンテナが壊れないようにする。
        /// 未設定でもキーボードで視線とアクションを操作できる。
        /// </summary>
        private void RegisterDjController(IContainerBuilder builder)
        {
            if (_djControllerInput != null)
            {
                builder.RegisterComponent(_djControllerInput);
                return;
            }
            
            Debug.LogWarning("[BusinessParty] DjControllerInput が未設定です。キーボードで操作できます。");
            builder.Register<DjControllerInput>(_ => null, Lifetime.Singleton);
        }
    }
}
