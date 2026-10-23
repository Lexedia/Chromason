using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Chromason.Colours;

[HarmonyPatch]
internal static class SabreColourOverride
{
    internal static (Color Left, Color Right)? Colours { get; set; }

    private static ColorType? _cutColourType;

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NoteCutCoreEffectsSpawner), nameof(NoteCutCoreEffectsSpawner.SpawnNoteCutEffect))]
    private static void BeginCutEffect(NoteController noteController)
    {
        _cutColourType = noteController.noteData.colorType;
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(NoteCutCoreEffectsSpawner), nameof(NoteCutCoreEffectsSpawner.SpawnNoteCutEffect))]
    private static void EndCutEffect()
    {
        _cutColourType = null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NoteCutParticlesEffect), nameof(NoteCutParticlesEffect.SpawnParticles))]
    private static void CutParticlesColour(ref Color32 color)
    {
        if (Colours is { } colours && _cutColourType is ColorType.ColorA or ColorType.ColorB)
        {
            var alpha = color.a;
            color = _cutColourType == ColorType.ColorB ? colours.Right : colours.Left;
            color.a = alpha;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SliderParticleInteractionEffect), nameof(SliderParticleInteractionEffect.Start))]
    private static void ArcSparksColour(SliderParticleInteractionEffect __instance)
    {
        if (Colours is { } colours)
        {
            __instance._startColor = __instance.colorType == ColorType.ColorB ? colours.Right : colours.Left;
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
