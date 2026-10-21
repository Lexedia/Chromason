using System.Runtime.CompilerServices;
using Chromason.Rendering;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace Chromason.Configuration;

internal class PluginConfig
{
    public static PluginConfig Instance { get; set; } = null!;

    public virtual bool HeadsetEnabled { get; set; } = true;
    public virtual bool DesktopEnabled { get; set; } = false;

    public virtual bool ChromaEventsDisabledInHeadset { get; set; } = true;
    public virtual bool ChromaEventsDisabledOnDesktop { get; set; } = true;

    public virtual bool EnvironmentEnhancementsDisabledInHeadset { get; set; } = true;
    public virtual bool EnvironmentEnhancementsDisabledOnDesktop { get; set; } = true;

    public virtual bool NoteColouringDisabledInHeadset { get; set; } = true;
    public virtual bool NoteColouringDisabledOnDesktop { get; set; } = true;

    public virtual bool MapColoursDisabledInHeadset { get; set; } = true;
    public virtual bool MapColoursDisabledOnDesktop { get; set; } = true;

    public virtual bool StaticLightsInHeadset { get; set; } = false;
    public virtual bool StaticLightsOnDesktop { get; set; } = false;

    public bool ChromaEventsDisabledFor(ViewKind view) =>
        For(view, ChromaEventsDisabledInHeadset, ChromaEventsDisabledOnDesktop);

    public bool EnvironmentEnhancementsDisabledFor(ViewKind view) =>
        For(view, EnvironmentEnhancementsDisabledInHeadset, EnvironmentEnhancementsDisabledOnDesktop);

    public bool NoteColouringDisabledFor(ViewKind view) =>
        For(view, NoteColouringDisabledInHeadset, NoteColouringDisabledOnDesktop);

    public bool StaticLightsFor(ViewKind view) =>
        For(view, StaticLightsInHeadset, StaticLightsOnDesktop);

    public bool MapColoursDisabledFor(ViewKind view) =>
        For(view, MapColoursDisabledInHeadset, MapColoursDisabledOnDesktop);

    public bool ChromaEventsDisabledAnywhere =>
        ChromaEventsDisabledFor(ViewKind.Headset) || ChromaEventsDisabledFor(ViewKind.Desktop);

    public bool EnvironmentEnhancementsDisabledAnywhere =>
        EnvironmentEnhancementsDisabledFor(ViewKind.Headset) || EnvironmentEnhancementsDisabledFor(ViewKind.Desktop);

    public bool VanillaLightsUseOwnColours =>
        (ChromaEventsDisabledFor(ViewKind.Headset) && MapColoursDisabledFor(ViewKind.Headset)) ||
        (ChromaEventsDisabledFor(ViewKind.Desktop) && MapColoursDisabledFor(ViewKind.Desktop));

    private bool For(ViewKind view, bool inHeadset, bool onDesktop) =>
        view == ViewKind.Headset ? HeadsetEnabled && inHeadset : DesktopEnabled && onDesktop;
}
