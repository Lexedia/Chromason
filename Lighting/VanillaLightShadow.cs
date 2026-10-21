using System;
using System.Collections.Generic;
using Chromason.Colours;
using Chromason.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Chromason.Lighting;

[HarmonyPatch(typeof(LightSwitchEventEffect))]
internal static class VanillaLightShadow
{
    private static readonly Dictionary<LightSwitchEventEffect, Color> Colours = [];

    private static ColorScheme? _ownScheme;

    internal static int ShadowedCount => Colours.Count;

    internal static bool TryGetColour(LightSwitchEventEffect effect, out Color colour) =>
        Colours.TryGetValue(effect, out colour);

    private static bool ShadowNeeded(PluginConfig config) => config.ChromaEventsDisabledAnywhere;

    [HarmonyReversePatch]
    [HarmonyPatch(nameof(LightSwitchEventEffect.Start))]
    private static void OriginalStart(LightSwitchEventEffect instance) => throw new NotImplementedException("Stub replaced by Harmony");

    [HarmonyPostfix]
    [HarmonyPatch(nameof(LightSwitchEventEffect.Start))]
    private static void StartShadow(LightSwitchEventEffect __instance, bool __runOriginal)
    {
        if (__runOriginal || !ShadowNeeded(PluginConfig.Instance))
        {
            return;
        }

        _ownScheme = PluginConfig.Instance.VanillaLightsUseOwnColours ? OwnColourSchemeCapture.Captured : null;

        Colours[__instance] = Color.clear;

        using var scope = BeginOwnScheme(__instance);
        try
        {
            OriginalStart(__instance);
        }
        catch (Exception exception)
        {
            Colours.Remove(__instance);
            Plugin.Log.Error($"Could not shadow {__instance.name}: {exception}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(LightSwitchEventEffect.HandleColorChangeBeatmapEvent))]
    private static void EventWithOwnScheme(LightSwitchEventEffect __instance, out LightColourSchemeSwap.SchemeSwapScope? __state)
    {
        __state = BeginOwnScheme(__instance);
    }

    [HarmonyFinalizer]
    [HarmonyPatch(nameof(LightSwitchEventEffect.HandleColorChangeBeatmapEvent))]
    private static void EventWithOwnSchemeDone(LightColourSchemeSwap.SchemeSwapScope? __state)
    {
        __state?.Dispose();
    }

    private static LightColourSchemeSwap.SchemeSwapScope? BeginOwnScheme(LightSwitchEventEffect effect) =>
        _ownScheme != null && Colours.ContainsKey(effect) ? LightColourSchemeSwap.Begin(effect, _ownScheme) : null;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(LightSwitchEventEffect.SetColor))]
    private static bool CaptureColour(LightSwitchEventEffect __instance, Color color)
    {
        if (!Colours.ContainsKey(__instance))
        {
            return true;
        }

        Colours[__instance] = color;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(LightSwitchEventEffect.OnDestroy))]
    private static void Forget(LightSwitchEventEffect __instance)
    {
        Colours.Remove(__instance);
    }
}
