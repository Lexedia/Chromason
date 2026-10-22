using System;
using System.Collections.Generic;
using Chromason.Configuration;
using Chromason.Environment;
using Chromason.Rendering;
using Heck.Animation;
using UnityEngine;
using Zenject;

namespace Chromason.Reverters;

internal class EnvironmentReverter : IViewReverter, ITickable, IDisposable
{
    private static readonly Vector3 HiddenOffset = new(0f, -10000f, 0f);

    private readonly PluginConfig _config;
    private readonly Dictionary<string, Track>? _tracks;

    private readonly List<Transform> _addedRoots = [];
    private readonly List<Transform> _removedRoots = [];
    private readonly List<MovedObject> _movedObjects = [];

    private FogState? _originalFog;
    private FogState _chromaFog;

    private bool _prepared;
    private Applied _applied;

    private EnvironmentReverter(PluginConfig config, [InjectOptional] Dictionary<string, Track>? tracks)
    {
        _config = config;
        _tracks = tracks;
    }

    public void Tick()
    {
        if (_prepared || !EnvironmentCapture.Completed || EnvironmentCapture.Before == null)
        {
            return;
        }

        _prepared = true;
        Prepare(EnvironmentCapture.Before);
    }

    public void Dispose()
    {
        EnvironmentCapture.Reset();
    }

    public void Apply(ViewKind view)
    {
        if (!_prepared)
        {
            return;
        }

        if (!_config.EnvironmentEnhancementsDisabledFor(view))
        {
            Offset(_removedRoots, HiddenOffset);
            _applied = Applied.RemovedHidden;
            return;
        }

        _applied = Applied.Vanilla;
        Offset(_addedRoots, HiddenOffset);

        foreach (var moved in _movedObjects)
        {
            moved.SwapToOriginal();
        }

        if (_originalFog is { } originalFog)
        {
            _chromaFog = FogState.CaptureGlobal();
            originalFog.Apply();
            _applied = Applied.VanillaWithFog;
        }
    }

    public void Restore()
    {
        switch (_applied)
        {
            case Applied.RemovedHidden:
                Offset(_removedRoots, -HiddenOffset);
                break;
            case Applied.Vanilla or Applied.VanillaWithFog:
                if (_applied == Applied.VanillaWithFog)
                {
                    _chromaFog.Apply();
                }

                for (var i = _movedObjects.Count - 1; i >= 0; i--)
                {
                    _movedObjects[i].SwapBack();
                }

                Offset(_addedRoots, -HiddenOffset);
                break;
        }

        _applied = Applied.Nothing;
    }

    private static void Offset(List<Transform> roots, Vector3 offset)
    {
        foreach (var root in roots)
        {
            if (root != null)
            {
                root.position += offset;
            }
        }
    }

    private void Prepare(EnvironmentSnapshot before)
    {
        var after = EnvironmentSnapshot.Take();
        _originalFog = before.Fog;

        var added = new HashSet<GameObject>();
        foreach (var gameObject in after.Objects.Keys)
        {
            if (!before.Objects.ContainsKey(gameObject))
            {
                added.Add(gameObject);
            }
        }

        var removed = new HashSet<GameObject>();
        foreach (var pair in before.Objects)
        {
            if (pair.Key != null && pair.Value.ActiveSelf && after.Objects.TryGetValue(pair.Key, out var state) && !state.ActiveSelf)
            {
                removed.Add(pair.Key);
            }
        }

        var tracked = new HashSet<GameObject>();
        if (_tracks != null)
        {
            foreach (var track in _tracks.Values)
            {
                tracked.UnionWith(track.GameObjects);
            }
        }

        foreach (var pair in after.Objects)
        {
            var gameObject = pair.Key;
            if (gameObject == null || removed.Contains(gameObject) || !before.Objects.TryGetValue(gameObject, out var original))
            {
                continue;
            }

            var moved = MovedObject.Create(gameObject.transform, original.Transform, pair.Value.Transform, tracked.Contains(gameObject));
            if (moved != null)
            {
                _movedObjects.Add(moved);
            }
        }

        CollectRoots(added, _addedRoots);
        CollectRoots(removed, _removedRoots);

        foreach (var gameObject in removed)
        {
            gameObject.SetActive(true);
        }

        var removedNames = new List<string>();
        foreach (var root in _removedRoots)
        {
            removedNames.Add(root.name);
        }

        Plugin.Log.Debug($"Environment: {added.Count} added ({_addedRoots.Count} roots), {removed.Count} removed ({string.Join(", ", removedNames)}), {_movedObjects.Count} moved or animated");
    }

    private static void CollectRoots(HashSet<GameObject> objects, List<Transform> roots)
    {
        foreach (var gameObject in objects)
        {
            var parent = gameObject.transform.parent;
            var hasAncestorInSet = false;
            while (parent != null)
            {
                if (objects.Contains(parent.gameObject))
                {
                    hasAncestorInSet = true;
                    break;
                }

                parent = parent.parent;
            }

            if (!hasAncestorInSet)
            {
                roots.Add(gameObject.transform);
            }
        }
    }

    private sealed class MovedObject
    {
        private readonly Transform _transform;
        private readonly TransformState _original;
        private readonly bool _position;
        private readonly bool _rotation;
        private readonly bool _scale;
        private TransformState _current;
        private bool _swapped;

        private MovedObject(Transform transform, TransformState original, bool position, bool rotation, bool scale)
        {
            _transform = transform;
            _original = original;
            _position = position;
            _rotation = rotation;
            _scale = scale;
        }

        internal static MovedObject? Create(Transform transform, TransformState before, TransformState after, bool tracked)
        {
            var position = tracked || before.LocalPosition != after.LocalPosition;
            var rotation = (tracked && !RotatedByGame(transform)) || before.LocalRotation != after.LocalRotation;
            var scale = tracked || before.LocalScale != after.LocalScale;

            return position || rotation || scale ? new MovedObject(transform, before, position, rotation, scale) : null;
        }

        private static bool RotatedByGame(Transform transform) =>
            transform.GetComponent<TrackLaneRing>() != null ||
            transform.GetComponent<LightRotationEventEffect>() != null ||
            (transform.parent != null && transform.parent.GetComponent<LightPairRotationEventEffect>() != null);

        internal void SwapToOriginal()
        {
            if (_transform == null)
            {
                return;
            }

            _current = TransformState.Capture(_transform);
            _swapped = true;

            if (_position)
            {
                _transform.localPosition = _original.LocalPosition;
            }

            if (_rotation)
            {
                _transform.localRotation = _original.LocalRotation;
            }

            if (_scale)
            {
                _transform.localScale = _original.LocalScale;
            }
        }

        internal void SwapBack()
        {
            if (!_swapped || _transform == null)
            {
                return;
            }

            _swapped = false;

            _transform.localPosition = _current.LocalPosition;
            _transform.localRotation = _current.LocalRotation;
            _transform.localScale = _current.LocalScale;
        }
    }

    private enum Applied
    {
        Nothing,
        RemovedHidden,
        Vanilla,
        VanillaWithFog
    }
}
