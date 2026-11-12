using System.Collections.Generic;
using Chroma.Lighting;
using Chromason.Rendering;
using HarmonyLib;
using UnityEngine;

namespace Chromason.Lighting;

[HarmonyPatch(typeof(ChromaIDColorTween), nameof(ChromaIDColorTween.SetColor))]
internal static class ChromaLightColourCache
{
    private static readonly Dictionary<ILightWithId, Color> Colours = [];

    internal static bool TryGetColour(ILightWithId light, out Color colour) => Colours.TryGetValue(light, out colour);

    internal static void Clear() => Colours.Clear();

    private static void Postfix(ChromaIDColorTween __instance, Color color)
    {
        if (ViewReverterRegistry.SwapsLights)
        {
            Colours[__instance._lightWithId] = color;
        }
    }
}
