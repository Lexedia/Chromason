using Chroma.Colorizer;
using Chromason.Configuration;
using Chromason.Lighting;
using Chromason.Rendering;
using UnityEngine;
using Zenject;

namespace Chromason.Reverters;

internal class LightColourReverter : LightSwapReverter
{
    private readonly PluginConfig _config;

    private bool _loggedFirstApply;

    private LightColourReverter(PluginConfig config, [InjectOptional] LightColorizerManager? manager)
        : base(manager)
    {
        _config = config;
    }

    public override bool ShouldApply(ViewKind view) =>
        Manager != null && _config.ChromaEventsDisabledFor(view) && !_config.StaticLightsFor(view);

    protected override bool TryGetColour(LightSwitchEventEffect effect, out Color colour) =>
        VanillaLightShadow.TryGetColour(effect, out colour);

    public override void Apply()
    {
        base.Apply();

        if (!_loggedFirstApply)
        {
            _loggedFirstApply = true;
            Plugin.Log.Debug($"Lights: {Manager!.Colorizers.Count} groups, {VanillaLightShadow.ShadowedCount} shadowed, {SwappedLightCount} lights shown in vanilla colours");
        }
    }
}
