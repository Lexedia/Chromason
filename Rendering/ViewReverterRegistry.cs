using System;
using System.Collections;
using System.Collections.Generic;
using Chromason.Reverters;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Chromason.Rendering;

internal class ViewReverterRegistry : IInitializable, IDisposable
{
    internal static ViewReverterRegistry? Current { get; private set; }

    private readonly List<IViewReverter> _reverters;
    private readonly List<IViewReverter> _applied = [];
    private readonly HashSet<IViewReverter> _failed = [];

    private Camera? _appliedBy;
    private EndOfFrameRestorer? _restorer;

    private ViewReverterRegistry(List<IViewReverter> reverters)
    {
        _reverters = reverters;
    }

    public void Initialize()
    {
        Current = this;

        _restorer = new GameObject("Chromason end of frame restorer").AddComponent<EndOfFrameRestorer>();
        _restorer.Registry = this;
    }

    public void Dispose()
    {
        Restore();

        if (_restorer != null)
        {
            Object.Destroy(_restorer.gameObject);
        }

        if (Current == this)
        {
            Current = null;
        }
    }

    internal void ApplyFor(Camera camera, ViewKind view)
    {
        if (_applied.Count != 0)
        {
            if (_appliedBy == camera)
            {
                return;
            }

            Restore();
        }

        _appliedBy = camera;

        foreach (var reverter in _reverters)
        {
            if (_failed.Contains(reverter))
            {
                continue;
            }

            _applied.Add(reverter);
            try
            {
                reverter.Apply(view);
            }
            catch (Exception exception)
            {
                Fail(reverter, "apply", exception);
            }
        }
    }

    internal void RestoreFor(Camera camera)
    {
        if (_appliedBy == camera)
        {
            Restore();
        }
    }

    internal void Restore()
    {
        for (var i = _applied.Count - 1; i >= 0; i--)
        {
            try
            {
                _applied[i].Restore();
            }
            catch (Exception exception)
            {
                Fail(_applied[i], "restore", exception);
            }
        }

        _applied.Clear();
        _appliedBy = null;
    }

    private void Fail(IViewReverter reverter, string step, Exception exception)
    {
        if (_failed.Add(reverter))
        {
            Plugin.Log.Error($"{reverter.GetType().Name} failed to {step}, turning it off for this level: {exception}");
        }
    }

    private class EndOfFrameRestorer : MonoBehaviour
    {
        private readonly WaitForEndOfFrame _endOfFrame = new();

        internal ViewReverterRegistry? Registry { get; set; }

        protected IEnumerator Start()
        {
            while (true)
            {
                yield return _endOfFrame;
                Registry?.Restore();
            }
        }
    }
}
