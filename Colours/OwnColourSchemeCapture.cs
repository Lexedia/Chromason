using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        Capture(_playerOverride ?? EnvironmentSchemeOf(__instance));
    }

    private static ColorScheme? EnvironmentSchemeOf(object setupData) => setupData switch
    {
        StandardLevelScenesTransitionSetupDataSO standard => new ColorScheme(standard.environmentInfo.colorScheme),
        MultiplayerLevelScenesTransitionSetupDataSO multiplayer => new ColorScheme(multiplayer._multiplayerEnvironmentInfo.colorScheme),
        _ => null
    };
#elif BEAT_SABER_1_42_0
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.GetColorInfo));
    }

    [HarmonyPriority(Priority.First)]
    private static void Postfix((bool, ColorScheme) __result)
    {
        Capture(__result.Item2);
    }

    [HarmonyPatch(typeof(MultiplayerLevelScenesTransitionSetupDataSO), nameof(MultiplayerLevelScenesTransitionSetupDataSO.Init))]
    private static class MultiplayerCapture
    {
        [HarmonyPriority(Priority.First)]
        private static void Postfix(MultiplayerLevelScenesTransitionSetupDataSO __instance, ColorScheme? overrideColorScheme)
        {
            Capture(overrideColorScheme ?? new ColorScheme(__instance._environmentInfo.colorScheme));
        }
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

    private static ColorScheme? SchemeOf(object setupData) => setupData switch
    {
        StandardLevelScenesTransitionSetupDataSO standard => standard.colorScheme,
        MultiplayerLevelScenesTransitionSetupDataSO multiplayer => multiplayer.colorScheme,
        _ => null
    };
#endif

    // Campaign levels have no map colours, so the scheme they play with is the player's own
    [HarmonyPatch]
    private static class MissionCapture
    {
        private static IEnumerable<MethodBase> TargetMethods() =>
            AccessTools.GetDeclaredMethods(typeof(MissionLevelScenesTransitionSetupDataSO))
                .Where(method => method.Name == "Init");

        [HarmonyPriority(Priority.First)]
        private static void Postfix(MissionLevelScenesTransitionSetupDataSO __instance)
        {
            var sceneSetupData = AccessTools.Field(typeof(MissionLevelScenesTransitionSetupDataSO), "_sceneSetupDataArray")?.GetValue(__instance) as IEnumerable;
            Capture(sceneSetupData?.OfType<GameplayCoreSceneSetupData>().FirstOrDefault()?.colorScheme);
        }
    }

    private static void Capture(ColorScheme? scheme)
    {
        Captured = scheme;
        SabreColourOverride.Colours = PluginConfig.Instance.OwnSabreColours && scheme != null
            ? (scheme.saberAColor, scheme.saberBColor)
            : null;
    }
}
