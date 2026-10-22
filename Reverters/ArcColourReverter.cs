using System.Collections.Generic;
using Chroma.Colorizer;
using Chromason.Colours;
using Chromason.Configuration;
using Chromason.Rendering;
using UnityEngine;
using Zenject;

namespace Chromason.Reverters;

internal class ArcColourReverter : IViewReverter
{
    private static readonly int ColourId = Shader.PropertyToID("_Color");

    private readonly PluginConfig _config;
    private readonly ColourSchemes _schemes;
    private readonly ColorManager _colourManager;
    private readonly SliderColorizerManager? _manager;

    private readonly List<(MaterialPropertyBlockController Controller, Color Colour)> _saved = [];

    private ArcColourReverter(
        PluginConfig config,
        ColourSchemes schemes,
        ColorManager colourManager,
        [InjectOptional] SliderColorizerManager? manager)
    {
        _config = config;
        _schemes = schemes;
        _colourManager = colourManager;
        _manager = manager;
    }

    public void Apply(ViewKind view)
    {
        if (_manager == null)
        {
            return;
        }

        var clearChroma = _config.NoteColouringDisabledFor(view) || _config.ChromaEventsDisabledFor(view);
        var useOwnColours = _schemes.HasMapColours && _config.MapColoursDisabledFor(view);
        if (!clearChroma && !useOwnColours)
        {
            return;
        }

        foreach (var pair in _manager.Colorizers)
        {
            var arc = pair.Key;
            if (arc == null || !arc.isActiveAndEnabled)
            {
                continue;
            }

            var colouriser = pair.Value;
            var chromaColoured = colouriser._color != null || colouriser.GlobalColorGetter != null;
            var colourType = arc.sliderData.colorType;

            var colour = clearChroma ? _colourManager.ColorForType(colourType) : arc._initColor;
            if (useOwnColours && (clearChroma || !chromaColoured))
            {
                colour = colourType == ColorType.ColorB ? _schemes.Own!.saberBColor : _schemes.Own!.saberAColor;
            }

            if (colour == arc._initColor)
            {
                continue;
            }

            var controller = arc._materialPropertyBlockController;
            var block = controller.materialPropertyBlock;
            _saved.Add((controller, block.GetColor(ColourId)));
            Write(controller, colour * arc.sliderIntensityEffect.colorIntensity);
        }
    }

    public void Restore()
    {
        foreach (var (controller, colour) in _saved)
        {
            if (controller != null)
            {
                Write(controller, colour);
            }
        }

        _saved.Clear();
    }

    private static void Write(MaterialPropertyBlockController controller, Color colour)
    {
        var block = controller.materialPropertyBlock;
        block.SetColor(ColourId, colour);
        foreach (var renderer in controller._renderers)
        {
            renderer.SetPropertyBlock(block);
        }
    }
}
