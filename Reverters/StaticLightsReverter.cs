// Parts adapted from ImBlindedByTheLights by Kinsi (MIT, see LICENCE), https://github.com/kinsi55/BeatSaber_ImBlindedByTheLights
// Based on LightSwitch and the light adapters (LightAdapters/).

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chroma.Colorizer;
using Chromason.Colours;
using Chromason.Configuration;
using Chromason.Environment;
using Chromason.Lighting;
using Chromason.Rendering;
using HarmonyLib;
using UnityEngine;
using Zenject;

namespace Chromason.Reverters;

internal class StaticLightsReverter : LightSwapReverter
{
    private const int StaticOnValue = 1;

    private static readonly Vector3 HiddenOffset = new(0f, -2000f, 0f);

    private readonly PluginConfig _config;
    private readonly ColourSchemes _schemes;
    private readonly Dictionary<BasicBeatmapEventType, (int Value, float Brightness)> _staticEvents;

    private readonly Dictionary<(LightSwitchEventEffect Effect, bool Own), Color> _colours = [];
    private readonly List<Pose> _staticPoses = [];
    private readonly List<(Transform Transform, Vector3 Position, Quaternion Rotation)> _savedPoses = [];
    private readonly List<ParticleSystemRenderer> _particles = [];
    private readonly List<ParticleSystemRenderer> _hiddenParticles = [];
    private readonly HashSet<Transform> _offLights = [];
    private readonly List<Transform> _hiddenLights = [];

    private bool _captured;
    private bool _capturedBeforeEnvironment;
    private bool _useOwnColours;

    private StaticLightsReverter(PluginConfig config, ColourSchemes schemes, DiContainer container, [InjectOptional] LightColorizerManager? manager)
        : base(manager)
    {
        _config = config;
        _schemes = schemes;
        _staticEvents = StaticEvents(container);
    }

    protected override bool HandlesVanillaLights => true;

    public override void Apply(ViewKind view)
    {
        if (!HasLights || !_config.StaticLightsFor(view))
        {
            return;
        }

        _useOwnColours = _schemes.HasMapColours && _config.MapColoursDisabledFor(view);

        if (NativeStaticLightsPatch.IsActive && !_useOwnColours)
        {
            return;
        }

        if (!_captured || (_capturedBeforeEnvironment && EnvironmentCapture.Completed))
        {
            _captured = true;
            _capturedBeforeEnvironment = !EnvironmentCapture.Completed;
            Capture();
        }

        SwapLights();

        foreach (var pose in _staticPoses)
        {
            if (pose.Transform == null)
            {
                continue;
            }

            _savedPoses.Add((pose.Transform, pose.Transform.localPosition, pose.Transform.localRotation));
            pose.Transform.localRotation = pose.Rotation;
            if (pose.Position is { } position)
            {
                pose.Transform.localPosition = position;
            }
        }

        foreach (var particles in _particles)
        {
            if (particles != null && !particles.forceRenderingOff)
            {
                particles.forceRenderingOff = true;
                _hiddenParticles.Add(particles);
            }
        }

        CollectOffLights();
        foreach (var light in _offLights)
        {
            light.position += HiddenOffset;
            _hiddenLights.Add(light);
        }
    }

    public override void Restore()
    {
        for (var i = _hiddenLights.Count - 1; i >= 0; i--)
        {
            if (_hiddenLights[i] != null)
            {
                _hiddenLights[i].position -= HiddenOffset;
            }
        }

        _hiddenLights.Clear();

        foreach (var particles in _hiddenParticles)
        {
            if (particles != null)
            {
                particles.forceRenderingOff = false;
            }
        }

        for (var i = _savedPoses.Count - 1; i >= 0; i--)
        {
            var (transform, position, rotation) = _savedPoses[i];
            if (transform != null)
            {
                transform.localPosition = position;
                transform.localRotation = rotation;
            }
        }

        _hiddenParticles.Clear();
        _savedPoses.Clear();

        base.Restore();
    }

    protected override bool TryGetColour(LightSwitchEventEffect lightSwitch, out Color colour)
    {
        if (!_staticEvents.TryGetValue(lightSwitch._event, out var staticEvent))
        {
            colour = Color.clear;
            return true;
        }

        var key = (lightSwitch, _useOwnColours);
        if (_colours.TryGetValue(key, out colour))
        {
            return true;
        }

        using (_useOwnColours ? LightColourSchemeSwap.Begin(lightSwitch, _schemes.Own!) : (LightColourSchemeSwap.SchemeSwapScope?)null)
        {
            colour = lightSwitch.GetNormalColor(staticEvent.Value, false);
            colour.a *= staticEvent.Brightness;
        }

        _colours[key] = colour;
        return true;
    }

    private bool IsOn(LightSwitchEventEffect lightSwitch) => _staticEvents.ContainsKey(lightSwitch._event);

    private static Dictionary<BasicBeatmapEventType, (int Value, float Brightness)> StaticEvents(DiContainer container)
    {
#if BEAT_SABER_1_29_0

        if (BetterStaticLightsEvents() is { } staticEvents)
        {
            Plugin.Log.Debug($"Static lights follow BetterStaticLights: {staticEvents.Count} light sets on");
            return staticEvents;
        }
#else
        if (BetterLegacyLightsEvents(container) is { } legacyEvents)
        {
            Plugin.Log.Debug($"Static lights follow BetterLegacyLights: {legacyEvents.Count} light sets on");
            return legacyEvents;
        }
#endif

        return new()
        {
            [BasicBeatmapEventType.Event0] = (StaticOnValue, 1f),
            [BasicBeatmapEventType.Event4] = (StaticOnValue, 1f)
        };
    }

#if BEAT_SABER_1_29_0
    private static Dictionary<BasicBeatmapEventType, (int Value, float Brightness)>? BetterStaticLightsEvents()
    {
        var pluginType = AccessTools.TypeByName("BetterStaticLights.Plugin");
        if (pluginType == null || !Harmony.HasAnyPatches("bs.Exo.BetterStaticLights") ||
            Traverse.Create(pluginType).Property("Instance").GetValue() is not { } plugin ||
            Traverse.Create(plugin).Field("Config").Field("lightSets").GetValue() is not IEnumerable lightSets)
        {
            return null;
        }

        var events = new Dictionary<BasicBeatmapEventType, (int Value, float Brightness)>();
        foreach (var item in lightSets)
        {
            var lightSet = Traverse.Create(item);
            if (lightSet.Property<bool>("Enabled").Value)
            {
                events[lightSet.Field<BasicBeatmapEventType>("EventType").Value] =
                    (lightSet.Property<bool>("UseSecondaryColor").Value ? 1 : 5, 1f);
            }
        }

        return events;
    }
#else
    private static Dictionary<BasicBeatmapEventType, (int Value, float Brightness)>? BetterLegacyLightsEvents(DiContainer container)
    {
        var configType = AccessTools.TypeByName("BetterLegacyLights.PluginConfig");
        var lightSetType = AccessTools.TypeByName("BetterLegacyLights.LightSet");
        if (configType == null || lightSetType == null || container.TryResolve(configType) is not { } config)
        {
            return null;
        }

        var events = new Dictionary<BasicBeatmapEventType, (int Value, float Brightness)>();
        foreach (var property in configType.GetProperties().Where(property => property.PropertyType == lightSetType))
        {
            var lightSet = Traverse.Create(property.GetValue(config));
            if (lightSet.Property<bool>("Enabled").Value)
            {
                events[lightSet.Property<BasicBeatmapEventType>("BeatmapEventType").Value] =
                    (lightSet.Property<int>("ColorMode").Value, lightSet.Property<float>("Brightness").Value);
            }
        }

        return events;
    }
#endif

    private void CollectOffLights()
    {
        _offLights.Clear();

        for (var i = 0; i < Groups.Count; i++)
        {
            var group = Groups[i];
            if (IsOn(group.Effect))
            {
                continue;
            }

            foreach (var light in group)
            {
                if (light is LightWithIds.LightWithId || light is not Component component || component == null)
                {
                    continue;
                }

                _offLights.Add(component.transform);
                if (light is TubeBloomPrePassLightWithId { _tubeBloomPrePassLight: { } tube } && tube != null)
                {
                    _offLights.Add(tube.transform);
                }
            }
        }
    }

    private void Capture()
    {
        _staticPoses.Clear();
        _particles.Clear();

        foreach (var rotation in Object.FindObjectsOfType<LightRotationEventEffect>())
        {
            _staticPoses.Add(new Pose(rotation._transform, null, rotation._startRotation));
        }

        foreach (var pair in Object.FindObjectsOfType<LightPairRotationEventEffect>())
        {
            AddCurrentPose(pair._transformL, false);
            AddCurrentPose(pair._transformR, false);
        }

        foreach (var ring in Object.FindObjectsOfType<TrackLaneRing>())
        {
            AddCurrentPose(ring._transform, true);
        }

        foreach (var effect in Object.FindObjectsOfType<ParticleSystemEventEffect>())
        {
            if (effect._particleSystem != null && effect._particleSystem.GetComponent<ParticleSystemRenderer>() is { } particles)
            {
                _particles.Add(particles);
            }
        }

        Plugin.Log.Debug($"Static lights: {_staticPoses.Count} moving parts held still, {_particles.Count} particle systems hidden");
    }

    private void AddCurrentPose(Transform? transform, bool includePosition)
    {
        if (transform != null)
        {
            _staticPoses.Add(new Pose(transform, includePosition ? transform.localPosition : null, transform.localRotation));
        }
    }

    private readonly struct Pose
    {
        internal readonly Transform Transform;
        internal readonly Vector3? Position;
        internal readonly Quaternion Rotation;

        internal Pose(Transform transform, Vector3? position, Quaternion rotation)
        {
            Transform = transform;
            Position = position;
            Rotation = rotation;
        }
    }
}
