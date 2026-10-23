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
    internal static bool IsActive { get; private set; }

#if BEAT_SABER_1_29_0
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.GetBeatmapDataFromSaveData));
    }

    private static void Prefix(BeatmapDifficulty beatmapDifficulty, ref PlayerSpecificSettings? playerSpecificSettings)
    {
        LoadStatic(beatmapDifficulty, ref playerSpecificSettings);
    }
#else
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.LoadBeatmapDataAsync));
        yield return AccessTools.Method(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.LoadBeatmapData));
    }

    private static void Prefix(BeatmapKey beatmapKey, ref PlayerSpecificSettings? playerSpecificSettings)
    {
        LoadStatic(beatmapKey.difficulty, ref playerSpecificSettings);
    }
#endif

    private static void LoadStatic(BeatmapDifficulty difficulty, ref PlayerSpecificSettings? playerSpecificSettings)
    {
        if (playerSpecificSettings == null)
        {
            IsActive = false;
            return;
        }

        var config = PluginConfig.Instance;
        if (config.StaticLightsFor(ViewKind.Headset) && config.StaticLightsFor(ViewKind.Desktop))
        {
            playerSpecificSettings = playerSpecificSettings.CopyWith(
                environmentEffectsFilterDefaultPreset: EnvironmentEffectsFilterPreset.NoEffects,
                environmentEffectsFilterExpertPlusPreset: EnvironmentEffectsFilterPreset.NoEffects);
        }

        IsActive = playerSpecificSettings.GetEnvironmentEffectsFilterPreset(difficulty) == EnvironmentEffectsFilterPreset.NoEffects;
    }
}
