using Chromason.UI;
using Zenject;

namespace Chromason.Installers;

internal class MenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<GameplaySetupTab>().AsSingle();
    }
}
