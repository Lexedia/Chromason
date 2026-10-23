using System.Collections.Generic;
using System.Reflection;
using Chroma.EnvironmentEnhancement;
using Chromason.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Chromason.Environment;

[HarmonyPatch]
internal static class EnvironmentCapture
{
    private const int StateAfterFirstYield = 1;

    internal static EnvironmentSnapshot? Before { get; private set; }

    internal static bool Completed { get; private set; }

    internal static List<GameObject> Geometry { get; } = [];

    internal static void Reset()
    {
        Before = null;
        Completed = false;
        Geometry.Clear();
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
        if (__result)
        {
            return;
        }

        Completed = true;

        if (Before == null && PluginConfig.Instance.EnvironmentEnhancementsDisabledAnywhere)
        {
            Plugin.Log.Warn("Chroma built the environment without a snapshot before it, its DelayedStart probably changed: environment changes won't be undone this level");
        }
    }

    [HarmonyPatch(typeof(GeometryFactory), nameof(GeometryFactory.Create))]
    private static class GeometryCapture
    {
        private static void Postfix(GameObject __result)
        {
            if (Before != null && __result != null)
            {
                Geometry.Add(__result);
            }
        }
    }
}
