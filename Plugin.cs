using System;
using System.Reflection;
using Chromason.Colours;
using Chromason.Configuration;
using Chromason.Installers;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace Chromason;

[Plugin(RuntimeOptions.SingleStartInit)]
internal class Plugin
{
    internal const string HarmonyId = "moe.lexedia.chromason";

    internal static IPALogger Log { get; private set; } = null!;

    internal static bool IsIBTTLInstalled =>
        IPA.Loader.PluginManager.GetPluginFromId("ImBlindedByTheLights") != null;

    private readonly Harmony _harmony = new(HarmonyId);

    [Init]
    public Plugin(IPALogger logger, IPA.Config.Config config, Zenjector zenjector)
    {
        Log = logger;
        PluginConfig.Instance = config.Generated<PluginConfig>();

        zenjector.UseLogger(logger);
        zenjector.Install<MenuInstaller>(Location.Menu);
        zenjector.Install<PlayerInstaller>(Location.Player);
    }

    [OnStart]
    public void OnStart()
    {
        PatchAll();
        NoteTweaksCompat.Initialise();

        if (IsIBTTLInstalled)
        {
            Log.Warn("ImBlindedByTheLights is installed: it changes lights per camera too and conflicts with Chromason, disable one of them");
        }
    }

    private void PatchAll()
    {
        foreach (var type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            try
            {
                _harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception exception)
            {
                Log.Error($"Could not patch {type.Name}, that part of Chromason won't work: {exception}");
            }
        }
    }

    [OnExit]
    public void OnExit()
    {
        _harmony.UnpatchSelf();
    }
}
