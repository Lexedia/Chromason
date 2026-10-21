using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using Chromason.Configuration;
using Zenject;

namespace Chromason.UI;

internal class GameplaySetupTab : IInitializable, IDisposable
{
    private const string TabName = "Chromason";
    private const string Resource = "Chromason.UI.GameplaySetupTab.bsml";

    private readonly GameplaySetup _gameplaySetup;

    private static PluginConfig Config => PluginConfig.Instance;

#if BEAT_SABER_1_29_0
    private GameplaySetupTab()
    {
        _gameplaySetup = GameplaySetup.instance;
    }
#else
    private GameplaySetupTab(GameplaySetup gameplaySetup)
    {
        _gameplaySetup = gameplaySetup;
    }
#endif

    public void Initialize()
    {
        _gameplaySetup.AddTab(TabName, Resource, this);
    }

    public void Dispose()
    {
        _gameplaySetup.RemoveTab(TabName);
    }

    [UIValue("ibbtl-installed")]
    private bool IsIBTTLInstalled => Plugin.IsIBTTLInstalled;

    [UIValue("headset-enabled")]
    private bool HeadsetEnabled
    {
        get => Config.HeadsetEnabled;
        set => Config.HeadsetEnabled = value;
    }

    [UIValue("desktop-enabled")]
    private bool DesktopEnabled
    {
        get => Config.DesktopEnabled;
        set => Config.DesktopEnabled = value;
    }

    [UIValue("chroma-events-headset")]
    private bool ChromaEventsDisabledInHeadset
    {
        get => Config.ChromaEventsDisabledInHeadset;
        set => Config.ChromaEventsDisabledInHeadset = value;
    }

    [UIValue("chroma-events-desktop")]
    private bool ChromaEventsDisabledOnDesktop
    {
        get => Config.ChromaEventsDisabledOnDesktop;
        set => Config.ChromaEventsDisabledOnDesktop = value;
    }

    [UIValue("environment-headset")]
    private bool EnvironmentEnhancementsDisabledInHeadset
    {
        get => Config.EnvironmentEnhancementsDisabledInHeadset;
        set => Config.EnvironmentEnhancementsDisabledInHeadset = value;
    }

    [UIValue("environment-desktop")]
    private bool EnvironmentEnhancementsDisabledOnDesktop
    {
        get => Config.EnvironmentEnhancementsDisabledOnDesktop;
        set => Config.EnvironmentEnhancementsDisabledOnDesktop = value;
    }

    [UIValue("note-colouring-headset")]
    private bool NoteColouringDisabledInHeadset
    {
        get => Config.NoteColouringDisabledInHeadset;
        set => Config.NoteColouringDisabledInHeadset = value;
    }

    [UIValue("note-colouring-desktop")]
    private bool NoteColouringDisabledOnDesktop
    {
        get => Config.NoteColouringDisabledOnDesktop;
        set => Config.NoteColouringDisabledOnDesktop = value;
    }

    [UIValue("map-colours-headset")]
    private bool MapColoursDisabledInHeadset
    {
        get => Config.MapColoursDisabledInHeadset;
        set => Config.MapColoursDisabledInHeadset = value;
    }

    [UIValue("map-colours-desktop")]
    private bool MapColoursDisabledOnDesktop
    {
        get => Config.MapColoursDisabledOnDesktop;
        set => Config.MapColoursDisabledOnDesktop = value;
    }

    [UIValue("static-lights-headset")]
    private bool StaticLightsInHeadset
    {
        get => Config.StaticLightsInHeadset;
        set => Config.StaticLightsInHeadset = value;
    }

    [UIValue("static-lights-desktop")]
    private bool StaticLightsOnDesktop
    {
        get => Config.StaticLightsOnDesktop;
        set => Config.StaticLightsOnDesktop = value;
    }
}
