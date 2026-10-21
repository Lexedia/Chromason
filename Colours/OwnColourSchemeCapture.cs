using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Chromason.Colours;

[HarmonyPatch]
internal static class OwnColourSchemeCapture
{
    internal static ColorScheme? Captured { get; private set; }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.InitColorInfo));
        yield return AccessTools.Method(typeof(MultiplayerLevelScenesTransitionSetupDataSO), nameof(MultiplayerLevelScenesTransitionSetupDataSO.InitColorInfo));
    }

    [HarmonyPriority(Priority.First)]
    private static void Postfix(object __instance)
    {
        Captured = __instance switch
        {
            StandardLevelScenesTransitionSetupDataSO standard => standard.colorScheme,
            MultiplayerLevelScenesTransitionSetupDataSO multiplayer => multiplayer.colorScheme,
            _ => null
        };
    }
}
