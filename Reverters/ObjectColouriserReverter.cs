using System.Collections.Generic;
using Chroma.Colorizer;
using Chromason.Rendering;
using UnityEngine;

namespace Chromason.Reverters;

internal abstract class ObjectColouriserReverter : IViewReverter
{
    private readonly List<ObjectColorizer> _colourisers = [];
    private readonly List<Color?> _savedColours = [];

    private bool _clearedGlobalColour;

    protected bool ClearChroma { get; private set; }

    protected bool UseOwnColours { get; private set; }

    protected abstract bool IsAvailable { get; }

    protected abstract bool ClearsChromaFor(ViewKind view);

    protected virtual bool UsesOwnColoursFor(ViewKind view) => false;

    protected virtual Color? OwnColour(ObjectColorizer colouriser) => null;

    protected abstract void CollectColourisers(List<ObjectColorizer> colourisers);

    protected abstract void ClearGlobalColour();

    protected abstract void RestoreGlobalColour();

    public virtual void Apply(ViewKind view)
    {
        ClearChroma = IsAvailable && ClearsChromaFor(view);
        UseOwnColours = IsAvailable && UsesOwnColoursFor(view);
        if (!ClearChroma && !UseOwnColours)
        {
            return;
        }

        if (ClearChroma)
        {
            ClearGlobalColour();
            _clearedGlobalColour = true;
        }

        CollectColourisers(_colourisers);

        foreach (var colouriser in _colourisers)
        {
            _savedColours.Add(colouriser._color);

            if (ClearChroma)
            {
                colouriser._color = null;
            }

            if (UseOwnColours && colouriser._color == null && colouriser.GlobalColorGetter == null)
            {
                colouriser._color = OwnColour(colouriser);
            }

            colouriser.Refresh();
        }
    }

    public virtual void Restore()
    {
        if (_clearedGlobalColour)
        {
            RestoreGlobalColour();
            _clearedGlobalColour = false;
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
