using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chromason.Configuration;
using Chromason.Rendering;
using HarmonyLib;

namespace Chromason.Colours;

[HarmonyPatch]
internal static class NativeMapColours
{
    private static readonly AccessTools.FieldRef<GameplayCoreSceneSetupData, ColorScheme> GameplaySchemeOf =
        AccessTools.FieldRefAccess<GameplayCoreSceneSetupData, ColorScheme>(nameof(GameplayCoreSceneSetupData.colorScheme));

    private static IEnumerable<MethodBase> TargetMethods() =>
        AccessTools.GetDeclaredMethods(typeof(StandardLevelScenesTransitionSetupDataSO))
            .Concat(AccessTools.GetDeclaredMethods(typeof(MultiplayerLevelScenesTransitionSetupDataSO)))
            .Where(method => method.Name == "Init");

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(LevelScenesTransitionSetupDataSO __instance)
    {
        var config = PluginConfig.Instance;
        if (!config.MapColoursDisabledFor(ViewKind.Headset) || !config.MapColoursDisabledFor(ViewKind.Desktop) ||
            OwnColourSchemeCapture.Captured is not { } own)
        {
            return;
        }

        var replaced = __instance switch
        {
            StandardLevelScenesTransitionSetupDataSO standard when standard.colorScheme != own => standard.colorScheme = own,
            MultiplayerLevelScenesTransitionSetupDataSO multiplayer when multiplayer.colorScheme != own => multiplayer.colorScheme = own,
            _ => null
        };

        if (replaced == null)
        {
            return;
        }

        if (__instance.gameplayCoreSceneSetupData is { } gameplay)
        {
            GameplaySchemeOf(gameplay) = own;
        }

        Plugin.Log.Debug($"Map colours off in both views: the level uses {own.colorSchemeId}");
    }
}
