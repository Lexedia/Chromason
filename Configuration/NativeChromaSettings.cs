using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chroma.Settings;
using Chromason.Rendering;
using HarmonyLib;
using Heck.SettingsSetter;

namespace Chromason.Configuration;

[HarmonyPatch]
internal static class NativeChromaSettings
{
    private static readonly AccessTools.FieldRef<SettableSetting<bool>, bool?> TemporaryOf =
        AccessTools.FieldRefAccess<SettableSetting<bool>, bool?>("_tempValue");

    private static readonly List<(SettableSetting<bool> Setting, bool? Previous)> Overridden = [];

    private static IEnumerable<MethodBase> TargetMethods() =>
        AccessTools.GetDeclaredMethods(typeof(StandardLevelScenesTransitionSetupDataSO))
            .Concat(AccessTools.GetDeclaredMethods(typeof(MultiplayerLevelScenesTransitionSetupDataSO)))
            .Where(method => method.Name == "Init");

    [HarmonyPriority(Priority.First)]
    private static void Prefix()
    {
        Release();

        var config = PluginConfig.Instance;
        Override(ChromaSettableSettings.ChromaEventsDisabledSetting,
            config.ChromaEventsDisabledFor(ViewKind.Headset) && config.ChromaEventsDisabledFor(ViewKind.Desktop));
        Override(ChromaSettableSettings.EnvironmentEnhancementsDisabledSetting,
            config.EnvironmentEnhancementsDisabledFor(ViewKind.Headset) && config.EnvironmentEnhancementsDisabledFor(ViewKind.Desktop));
        Override(ChromaSettableSettings.NoteColoringDisabledSetting,
            config.NoteColouringDisabledFor(ViewKind.Headset) && config.NoteColouringDisabledFor(ViewKind.Desktop));

        if (Overridden.Count != 0)
        {
            Plugin.Log.Debug($"Turned off in Chroma for this level: {string.Join(", ", Overridden.Select(entry => entry.Setting.FieldName))}");
        }
    }

    private static void Override(SettableSetting<bool> setting, bool disabledEverywhere)
    {
        if (!disabledEverywhere)
        {
            return;
        }

        Overridden.Add((setting, TemporaryOf(setting)));
        setting.SetTemporary(true);
    }

    private static void Release()
    {
        foreach (var (setting, previous) in Overridden)
        {
            if (TemporaryOf(setting) == true)
            {
                setting.SetTemporary(previous);
            }
        }

        Overridden.Clear();
    }

    [HarmonyPatch]
    private static class MenuReturn
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("GameScenesManager"), "PopScenes");

        private static void Prefix()
        {
            Release();
        }
    }
}
