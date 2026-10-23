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

    private List<Transform>? _offsetRoots;
    private int _offsetCount;
    private int _swappedCount;
    private bool _fogSwapped;

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

        var vanilla = _config.EnvironmentEnhancementsDisabledFor(view);
        _offsetRoots = vanilla ? _addedRoots : _removedRoots;
        for (; _offsetCount < _offsetRoots.Count; _offsetCount++)
        {
            if (_offsetRoots[_offsetCount] is { } root && root != null)
            {
                root.position += HiddenOffset;
            }
        }

        if (!vanilla)
        {
            return;
        }

        for (; _swappedCount < _movedObjects.Count; _swappedCount++)
        {
            _movedObjects[_swappedCount].SwapToOriginal();
        }

        if (_originalFog is { } originalFog)
        {
            _chromaFog = FogState.CaptureGlobal();
            _fogSwapped = true;
            originalFog.Apply();
        }
    }

    public void Restore()
    {
        if (_fogSwapped)
        {
            _chromaFog.Apply();
            _fogSwapped = false;
        }

        for (var i = _swappedCount - 1; i >= 0; i--)
        {
            _movedObjects[i].SwapBack();
        }

        _swappedCount = 0;

        if (_offsetRoots != null)
        {
            for (var i = _offsetCount - 1; i >= 0; i--)
            {
                if (_offsetRoots[i] is { } root && root != null)
                {
                    root.position -= HiddenOffset;
                }
            }
        }

        _offsetRoots = null;
        _offsetCount = 0;
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

        foreach (var gameObject in EnvironmentCapture.Geometry)
        {
            if (gameObject != null)
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
            if (_transform == null)
            {
                return;
            }

            _transform.localPosition = _current.LocalPosition;
            _transform.localRotation = _current.LocalRotation;
            _transform.localScale = _current.LocalScale;
        }
    }
}
