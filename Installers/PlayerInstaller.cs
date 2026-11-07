using Chromason.Colours;
using Chromason.Configuration;
using Chromason.Rendering;
using Chromason.Reverters;
using Zenject;

namespace Chromason.Installers;

internal class PlayerInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInstance(PluginConfig.Instance);
        Container.BindInterfacesAndSelfTo<ColourSchemes>().AsSingle().NonLazy();

        if (NoteTweaksCompat.Available)
        {
            Container.BindInterfacesTo<NoteTweaksSpawnTracker>().AsSingle();
        }

        Container.BindInterfacesTo<NoteColourReverter>().AsSingle();
        Container.BindInterfacesTo<BombColourReverter>().AsSingle();
        Container.BindInterfacesTo<ObstacleColourReverter>().AsSingle();
        Container.BindInterfacesTo<ArcColourReverter>().AsSingle();
        Container.BindInterfacesTo<LightColourReverter>().AsSingle();
        Container.BindInterfacesTo<StaticLightsReverter>().AsSingle();
        Container.BindInterfacesTo<EnvironmentReverter>().AsSingle();
        Container.BindInterfacesTo<VanillaMapColourReverter>().AsSingle();
        Container.BindInterfacesTo<VanillaLightColourReverter>().AsSingle();

        Container.BindInterfacesTo<ViewReverterRegistry>().AsSingle();
    }
}
