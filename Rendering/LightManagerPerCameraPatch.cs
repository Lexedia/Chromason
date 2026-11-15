using HarmonyLib;

namespace Chromason.Rendering;


[HarmonyPatch(typeof(LightManager), nameof(LightManager.OnCameraPreRender))]
internal static class LightManagerPerCameraPatch
{
    private static void Prefix(LightManager __instance)
    {
        if (ViewReverterRegistry.SwapsLights)
        {
            __instance.lastRefreshFrameNum = -1;
        }
    }
}
