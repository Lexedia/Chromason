using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Chromason.Colours;

[HarmonyPatch]
internal static class SabreColourOverride
{
    internal static (Color Left, Color Right)? Colours { get; set; }

    private static Color ColourFor(SaberType type, (Color Left, Color Right) colours) =>
        type == SaberType.SaberB ? colours.Right : colours.Left;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ColorManager), nameof(ColorManager.ColorForSaberType))]
    private static void SchemeColour(SaberType type, ref Color __result)
    {
        if (Colours is { } colours)
        {
            __result = ColourFor(type, colours);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ColorManager), nameof(ColorManager.EffectsColorForSaberType))]
    private static void EffectsColour(SaberType type, ref Color __result)
    {
        if (Colours is { } colours)
        {
            Color.RGBToHSV(ColourFor(type, colours), out var hue, out var saturation, out _);
            __result = Color.HSVToRGB(hue, saturation, 1f);
        }
    }

    [HarmonyPatch]
    private static class SiraUtilSetColour
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("SiraUtil.Extras.SaberExtensions"), "SetColor", [typeof(SaberModelController), typeof(Color)]);

        [HarmonyPriority(Priority.First)]
        private static void Prefix(SaberModelController saberModelController, ref Color color)
        {
            if (Colours is { } colours && saberModelController.GetComponentInParent<Saber>() is { } saber)
            {
                color = ColourFor(saber.saberType, colours);
            }
        }
    }
}
