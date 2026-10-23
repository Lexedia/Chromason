using System.Collections.Generic;
using System.Reflection;
using Chromason.Configuration;
using HarmonyLib;

namespace Chromason.Colours;

[HarmonyPatch]
internal static class OwnColourSchemeCapture
{
    internal static ColorScheme? Captured { get; private set; }

#if BEAT_SABER_1_29_0
    private static ColorScheme? _playerOverride;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.DeclaredMethod(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.Init));
        yield return AccessTools.DeclaredMethod(typeof(MultiplayerLevelScenesTransitionSetupDataSO), nameof(MultiplayerLevelScenesTransitionSetupDataSO.Init));
    }

    [HarmonyPriority(Priority.First)]
    private static void Prefix(ColorScheme? overrideColorScheme)
    {
        _playerOverride = overrideColorScheme;
    }

    [HarmonyPriority(Priority.First)]
    private static void Postfix(object __instance)
    {
        Capture(_playerOverride ?? SchemeOf(__instance));
    }
#else
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.InitColorInfo));
        yield return AccessTools.Method(typeof(MultiplayerLevelScenesTransitionSetupDataSO), nameof(MultiplayerLevelScenesTransitionSetupDataSO.InitColorInfo));
    }

    [HarmonyPriority(Priority.First)]
    private static void Postfix(object __instance)
    {
        Capture(SchemeOf(__instance));
    }
#endif

    private static void Capture(ColorScheme? scheme)
    {
        Captured = scheme;
        SabreColourOverride.Colours = PluginConfig.Instance.OwnSabreColours && scheme != null
            ? (scheme.saberAColor, scheme.saberBColor)
            : null;
    }

    private static ColorScheme? SchemeOf(object setupData) => setupData switch
    {
        StandardLevelScenesTransitionSetupDataSO standard => standard.colorScheme,
        MultiplayerLevelScenesTransitionSetupDataSO multiplayer => multiplayer.colorScheme,
        _ => null
    };
}
