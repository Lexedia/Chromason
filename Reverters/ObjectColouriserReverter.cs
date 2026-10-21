using System.Collections.Generic;
using Chroma.Colorizer;
using Chromason.Rendering;
using UnityEngine;

namespace Chromason.Reverters;

internal abstract class ObjectColouriserReverter : IViewReverter
{
    private readonly List<ObjectColorizer> _colourisers = [];
    private readonly List<Color?> _savedColours = [];

    private bool _clearChroma;
    private bool _useOwnColours;

    protected bool ClearChroma => _clearChroma;

    protected bool UseOwnColours => _useOwnColours;

    protected abstract bool IsAvailable { get; }

    protected abstract bool ClearsChromaFor(ViewKind view);

    protected virtual bool UsesOwnColoursFor(ViewKind view) => false;

    protected virtual Color? OwnColour(ObjectColorizer colouriser) => null;

    protected abstract void CollectColourisers(List<ObjectColorizer> colourisers);

    protected abstract void ClearGlobalColour();

    protected abstract void RestoreGlobalColour();

    public bool ShouldApply(ViewKind view)
    {
        if (!IsAvailable)
        {
            return false;
        }

        _clearChroma = ClearsChromaFor(view);
        _useOwnColours = UsesOwnColoursFor(view);
        return _clearChroma || _useOwnColours;
    }

    public virtual void Apply()
    {
        if (_clearChroma)
        {
            ClearGlobalColour();
        }

        CollectColourisers(_colourisers);

        foreach (var colouriser in _colourisers)
        {
            _savedColours.Add(colouriser._color);

            if (_clearChroma)
            {
                colouriser._color = null;
            }

            if (_useOwnColours && colouriser._color == null && colouriser.GlobalColorGetter == null)
            {
                colouriser._color = OwnColour(colouriser);
            }

            colouriser.Refresh();
        }
    }

    public virtual void Restore()
    {
        if (_clearChroma)
        {
            RestoreGlobalColour();
        }

        for (var i = 0; i < _savedColours.Count; i++)
        {
            _colourisers[i]._color = _savedColours[i];
            _colourisers[i].Refresh();
        }

        _colourisers.Clear();
        _savedColours.Clear();
    }
}
