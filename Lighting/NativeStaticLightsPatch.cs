// Parts adapted from ImBlindedByTheLights by Kinsi (MIT, see LICENCE), https://github.com/kinsi55/BeatSaber_ImBlindedByTheLights
// Based on BasegameStaticLights.

using System.Collections.Generic;
using System.Reflection;
using Chromason.Configuration;
using Chromason.Rendering;
using HarmonyLib;

namespace Chromason.Lighting;

[HarmonyPatch]
internal static class NativeStaticLightsPatch
{
#if BEAT_SABER_1_29_0
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.GetBeatmapDataFromSaveData));
    }

    private static void Prefix(ref PlayerSpecificSettings? playerSpecificSettings)
    {
        LoadStatic(ref playerSpecificSettings);
    }
#else
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.LoadBeatmapDataAsync));
        yield return AccessTools.Method(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.LoadBeatmapData));
    }

    private static void Prefix(ref PlayerSpecificSettings? playerSpecificSettings)
    {
        LoadStatic(ref playerSpecificSettings);
    }
#endif

    internal static bool IsActiveFor(GameplayCoreSceneSetupData setupData)
    {
        if (setupData.playerSpecificSettings is not { } playerSpecificSettings)
        {
            return false;
        }

#if BEAT_SABER_1_29_0
        var difficulty = setupData.difficultyBeatmap.difficulty;
#else
        var difficulty = setupData.beatmapKey.difficulty;
#endif
        return IsForced(PluginConfig.Instance) ||
               playerSpecificSettings.GetEnvironmentEffectsFilterPreset(difficulty) == EnvironmentEffectsFilterPreset.NoEffects;
    }

    private static bool IsForced(PluginConfig config) =>
        config.StaticLightsFor(ViewKind.Headset) && config.StaticLightsFor(ViewKind.Desktop);

    private static void LoadStatic(ref PlayerSpecificSettings? playerSpecificSettings)
    {
        if (playerSpecificSettings != null && IsForced(PluginConfig.Instance))
        {
            playerSpecificSettings = playerSpecificSettings.CopyWith(
                environmentEffectsFilterDefaultPreset: EnvironmentEffectsFilterPreset.NoEffects,
                environmentEffectsFilterExpertPlusPreset: EnvironmentEffectsFilterPreset.NoEffects);
        }
    }
}
