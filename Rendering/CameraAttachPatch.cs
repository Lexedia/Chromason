// Parts adapted from ImBlindedByTheLights by Kinsi (MIT, see LICENCE), https://github.com/kinsi55/BeatSaber_ImBlindedByTheLights
// Port of InterceptCameraRenders.

using HarmonyLib;
using UnityEngine;

namespace Chromason.Rendering;

[HarmonyPatch(typeof(BloomPrePass), nameof(BloomPrePass.Awake))]
internal static class CameraAttachPatch
{
    private static void Postfix(BloomPrePass __instance)
    {
        if (__instance.GetComponent<Camera>() == null || __instance.GetComponent<ViewSwitchBehaviour>() != null)
        {
            return;
        }

        __instance.gameObject.AddComponent<ViewSwitchBehaviour>();
    }
}
