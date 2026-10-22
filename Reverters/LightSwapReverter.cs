// Parts adapted from ImBlindedByTheLights by Kinsi (MIT, see LICENCE), https://github.com/kinsi55/BeatSaber_ImBlindedByTheLights
// Based on the light adapters (LightAdapters/TubeBloomPrePassLight.cs, LightmapLightsWithIds.cs).

using System;
using System.Collections.Generic;
using Chroma.Colorizer;
using Chroma.Lighting;
using Chromason.Lighting;
using Chromason.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Chromason.Reverters;

internal abstract class LightSwapReverter(LightColorizerManager? manager) : IViewReverter, IDisposable
{
    private readonly List<LightGroup> _groups = [];
    private readonly Dictionary<ChromaLightSwitchEventEffect, List<ILightWithId>> _chromaLights = [];
    private readonly List<(ILightWithId Light, Color Colour)> _swappedLights = [];

    private readonly HashSet<LightWithIds> _deferredParents = [];

    private readonly HashSet<BloomPrePassLight> _deferredBloomLights = [];
    private readonly Dictionary<ILightWithId, BloomPrePassLight?> _bloomLightLookup = [];

    private LightWithIdManager? _lightManager;
    private LightSwitchEventEffect[]? _vanillaEffects;

    protected LightColorizerManager? Manager { get; } = manager;

    protected virtual bool HandlesVanillaLights => false;

    protected bool HasLights => Manager != null || HandlesVanillaLights;

    protected int SwappedLightCount => _swappedLights.Count;

    protected IReadOnlyList<LightGroup> Groups => _groups;

    public abstract void Apply(ViewKind view);

    protected abstract bool TryGetColour(LightSwitchEventEffect effect, out Color colour);

    protected void SwapLights()
    {
        CollectGroups();

        foreach (var group in _groups)
        {
            if (!TryGetColour(group.Effect, out var colour))
            {
                continue;
            }

            foreach (var light in group.Lights)
            {
                if (light is not { isRegistered: true } || !TryGetRealColour(group, light, out var realColour))
                {
                    continue;
                }

                light.ColorWasSet(colour);
                _swappedLights.Add((light, realColour));

                if (light is LightWithIds.LightWithId { _parentLightWithIds: { } parent })
                {
                    _deferredParents.Add(parent);
                }
                else if (GetBloomLight(light) is { } bloomLight)
                {
                    _deferredBloomLights.Add(bloomLight);
                }
            }
        }

        ProcessDeferred();
    }

    public virtual void Restore()
    {
        foreach (var (light, colour) in _swappedLights)
        {
            light.ColorWasSet(colour);
        }

        ProcessDeferred();

        _swappedLights.Clear();
        _deferredParents.Clear();
        _deferredBloomLights.Clear();
    }

    public void Dispose()
    {
        ChromaLightColourCache.Clear();
    }

    private void CollectGroups()
    {
        _groups.Clear();

        if (Manager != null)
        {
            foreach (var colouriser in Manager.Colorizers.Values)
            {
                var effect = colouriser.ChromaLightSwitchEventEffect;
                if (!_chromaLights.TryGetValue(effect, out var lights))
                {
                    lights = [];
                    _chromaLights[effect] = lights;
                }

                lights.Clear();
                foreach (var light in effect.ColorTweens.Keys)
                {
                    lights.Add(light);
                }

                _groups.Add(new LightGroup(effect.LightSwitchEventEffect, lights));
            }

            return;
        }

        if (!HandlesVanillaLights)
        {
            return;
        }

        _lightManager ??= Object.FindObjectOfType<LightWithIdManager>();
        _vanillaEffects ??= Object.FindObjectsOfType<LightSwitchEventEffect>();
        if (_lightManager == null)
        {
            return;
        }

        foreach (var effect in _vanillaEffects)
        {
            if (effect != null && _lightManager._lights[effect._lightsID] is { } lights)
            {
                _groups.Add(new LightGroup(effect, lights));
            }
        }
    }

    private bool TryGetRealColour(LightGroup group, ILightWithId light, out Color colour)
    {
        if (Manager != null)
        {
            return ChromaLightColourCache.TryGetColour(light, out colour);
        }

        colour = _lightManager!._colors[group.Effect._lightsID] ?? Color.clear;
        return true;
    }

    private BloomPrePassLight? GetBloomLight(ILightWithId light)
    {
        if (_bloomLightLookup.TryGetValue(light, out var bloomLight))
        {
            return bloomLight;
        }

        bloomLight = light switch
        {
            TubeBloomPrePassLightWithId tube => tube._tubeBloomPrePassLight,
            Component component => component.GetComponent<BloomPrePassLight>(),
            _ => null
        };

        _bloomLightLookup[light] = bloomLight;
        return bloomLight;
    }

    private void ProcessDeferred()
    {
        foreach (var bloomLight in _deferredBloomLights)
        {
            bloomLight?.Refresh();
        }

        foreach (var parent in _deferredParents)
        {
            if (parent == null)
            {
                continue;
            }

            parent._childrenColorWasSet = false;
            parent.ProcessNewColorData();
        }
    }

    protected readonly struct LightGroup
    {
        internal readonly LightSwitchEventEffect Effect;
        internal readonly List<ILightWithId> Lights;

        internal LightGroup(LightSwitchEventEffect effect, List<ILightWithId> lights)
        {
            Effect = effect;
            Lights = lights;
        }
    }
}
