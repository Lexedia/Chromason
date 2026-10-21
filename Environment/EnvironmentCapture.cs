using System.Reflection;
using Chroma.EnvironmentEnhancement;
using Chromason.Configuration;
using HarmonyLib;

namespace Chromason.Environment;

[HarmonyPatch]
internal static class EnvironmentCapture
{
    private const int StateAfterFirstYield = 1;

    internal static EnvironmentSnapshot? Before { get; private set; }

    internal static bool Completed { get; private set; }

    internal static void Reset()
    {
        Before = null;
        Completed = false;
    }

    private static MethodBase TargetMethod() =>
        AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(EnvironmentEnhancementManager), nameof(EnvironmentEnhancementManager.DelayedStart)));

    private static void Prefix(object __instance)
    {
        if (!PluginConfig.Instance.EnvironmentEnhancementsDisabledAnywhere)
        {
            return;
        }

        if (Traverse.Create(__instance).Field<int>("<>1__state").Value != StateAfterFirstYield)
        {
            return;
        }

        Before = EnvironmentSnapshot.Take();
        Completed = false;
    }

    private static void Postfix(bool __result)
    {
        if (!__result)
        {
            Completed = true;
        }
    }
}
