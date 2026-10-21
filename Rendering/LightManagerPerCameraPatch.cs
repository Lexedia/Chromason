using HarmonyLib;

namespace Chromason.Rendering;


[HarmonyPatch(typeof(LightManager), nameof(LightManager.OnCameraPreRender))]
internal static class LightManagerPerCameraPatch
{
    private static void Prefix(LightManager __instance)
    {
        if (ViewReverterRegistry.Current != null)
        {
            __instance.lastRefreshFrameNum = -1;
        }
    }
}
