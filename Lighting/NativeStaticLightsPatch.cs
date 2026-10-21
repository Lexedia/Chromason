// Parts adapted from ImBlindedByTheLights by Kinsi (MIT, see LICENCE), https://github.com/kinsi55/BeatSaber_ImBlindedByTheLights
// Based on BasegameStaticLights.

using System.Collections.Generic;
using System.Reflection;
using Chromason.Configuration;
using Chromason.Rendering;
using HarmonyLib;

namespace Chromason.Lighting;

// TODO: 1.29 1.29 1.29 1.29 1.29 AAAAAAAAAAAh
#if !BEAT_SABER_1_29_0
[HarmonyPatch]
#endif
internal static class NativeStaticLightsPatch
{
    internal static bool IsActive { get; private set; }

#if !BEAT_SABER_1_29_0
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.LoadBeatmapDataAsync));
        yield return AccessTools.Method(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.LoadBeatmapData));
    }

    private static void Prefix(BeatmapKey beatmapKey, ref PlayerSpecificSettings? playerSpecificSettings)
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

        IsActive = playerSpecificSettings.GetEnvironmentEffectsFilterPreset(beatmapKey.difficulty) == EnvironmentEffectsFilterPreset.NoEffects;
    }
#endif
}
