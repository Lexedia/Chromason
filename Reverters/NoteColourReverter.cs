using System.Collections.Generic;
using Chroma.Colorizer;
using Chromason.Colours;
using Chromason.Configuration;
using Chromason.Rendering;
using UnityEngine;
using Zenject;

namespace Chromason.Reverters;

internal class NoteColourReverter : ObjectColouriserReverter
{
    private readonly PluginConfig _config;
    private readonly ColourSchemes _schemes;
    private readonly NoteColorizerManager? _manager;
    private readonly Color?[] _savedGlobalColours = new Color?[2];
    private readonly List<NoteTweaksCompat.NoteVariants> _swappedNotes = [];

    private NoteColourReverter(PluginConfig config, ColourSchemes schemes, [InjectOptional] NoteColorizerManager? manager)
    {
        _config = config;
        _schemes = schemes;
        _manager = manager;
    }

    protected override bool IsAvailable => _manager != null;

    protected override bool ClearsChromaFor(ViewKind view) =>
        _config.NoteColouringDisabledFor(view) || _config.ChromaEventsDisabledFor(view);

    protected override bool UsesOwnColoursFor(ViewKind view) => _schemes.HasMapColours && _config.MapColoursDisabledFor(view);

    protected override Color? OwnColour(ObjectColorizer colouriser) =>
        ((NoteColorizer)colouriser).ColorType switch
        {
            ColorType.ColorA => _schemes.Own!.saberAColor,
            ColorType.ColorB => _schemes.Own!.saberBColor,
            _ => null
        };

    protected override void CollectColourisers(List<ObjectColorizer> colourisers)
    {
        foreach (var pair in _manager!.Colorizers)
        {
            if (pair.Key.isActiveAndEnabled && !NoteTweaksCompat.TryGetVariants(pair.Key, out _))
            {
                colourisers.Add(pair.Value);
            }
        }
    }

    public override void Apply(ViewKind view)
    {
        base.Apply(view);

        if ((!ClearChroma && !UseOwnColours) || !NoteTweaksCompat.Available)
        {
            return;
        }

        foreach (var pair in _manager!.Colorizers)
        {
            if (!pair.Key.isActiveAndEnabled || !NoteTweaksCompat.TryGetVariants(pair.Key, out var variants))
            {
                continue;
            }

            var colouriser = pair.Value;
            var showsSchemeColour = ClearChroma || (colouriser._color == null && colouriser.GlobalColorGetter == null);
            if (!showsSchemeColour)
            {
                continue;
            }

            var colours = UseOwnColours && variants.Own != null ? variants.Own : ClearChroma ? variants.Scheme : null;
            if (colours == null)
            {
                continue;
            }

            NoteTweaksCompat.Read(variants.Controllers, variants.Saved);
            _swappedNotes.Add(variants);
            NoteTweaksCompat.Write(variants.Controllers, colours);
        }
    }

    public override void Restore()
    {
        foreach (var variants in _swappedNotes)
        {
            NoteTweaksCompat.Write(variants.Controllers, variants.Saved);
        }

        _swappedNotes.Clear();

        base.Restore();
    }

    protected override void ClearGlobalColour()
    {
        var globalColours = _manager!.GlobalColor;
        for (var i = 0; i < _savedGlobalColours.Length; i++)
        {
            _savedGlobalColours[i] = globalColours[i];
            globalColours[i] = null;
        }
    }

    protected override void RestoreGlobalColour()
    {
        var globalColours = _manager!.GlobalColor;
        for (var i = 0; i < _savedGlobalColours.Length; i++)
        {
            globalColours[i] = _savedGlobalColours[i];
        }
    }
}
