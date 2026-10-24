using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chroma.Colorizer;
using Chromason.Colours;
using Chromason.Configuration;
using Chromason.Lighting;
using Chromason.Rendering;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

namespace Chromason.Reverters;

internal class VanillaLightColourReverter : LightSwapReverter, IInitializable
{
    private static readonly FieldInfo[] TwinFields = [.. AccessTools.GetDeclaredFields(typeof(LightSwitchEventEffect)).Where(field => !field.IsStatic && field.FieldType != typeof(BeatmapDataCallbackWrapper))];

    private readonly PluginConfig _config;
    private readonly ColourSchemes _schemes;

    private readonly Dictionary<LightSwitchEventEffect, LightSwitchEventEffect> _twins = [];

    private VanillaLightColourReverter(PluginConfig config, ColourSchemes schemes, [InjectOptional] LightColorizerManager? manager)
        : base(manager)
    {
        _config = config;
        _schemes = schemes;
    }

    protected override bool HandlesVanillaLights => true;

    public void Initialize()
    {
        var ownScheme = OwnColourSchemeCapture.Captured;
        var wanted = _config.MapColoursDisabledFor(ViewKind.Headset) || _config.MapColoursDisabledFor(ViewKind.Desktop);
        if (Manager != null || !_schemes.HasMapColours || ownScheme == null || !wanted)
        {
            return;
        }

        foreach (var effect in Object.FindObjectsOfType<LightSwitchEventEffect>())
        {
            var holder = new GameObject($"Chromason twin of {effect.name}");
            holder.SetActive(false);
            SceneManager.MoveGameObjectToScene(holder, effect.gameObject.scene);

            var twin = holder.AddComponent<LightSwitchEventEffect>();
            foreach (var field in TwinFields)
            {
                field.SetValue(twin, field.GetValue(effect));
            }

            VanillaLightShadow.ShadowTwin(twin, ownScheme);
            _twins[effect] = twin;
        }
    }

    public override void Dispose()
    {
        foreach (var twin in _twins.Values)
        {
            if (twin == null)
            {
                continue;
            }

            twin.OnDestroy();
            Object.Destroy(twin.gameObject);
        }

        _twins.Clear();
        base.Dispose();
    }

    public override void Apply(ViewKind view)
    {
        if (_twins.Count == 0 || !_config.MapColoursDisabledFor(view) || _config.StaticLightsFor(view))
        {
            return;
        }

        SwapLights();
    }

    protected override bool TryGetColour(LightSwitchEventEffect effect, out Color colour)
    {
        colour = default;
        return _twins.TryGetValue(effect, out var twin) && VanillaLightShadow.TryGetColour(twin, out colour);
    }
}
