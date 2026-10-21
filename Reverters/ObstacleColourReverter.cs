using System.Collections.Generic;
using Chroma.Colorizer;
using Chromason.Colours;
using Chromason.Configuration;
using Chromason.Rendering;
using UnityEngine;
using Zenject;

namespace Chromason.Reverters;

internal class ObstacleColourReverter : ObjectColouriserReverter
{
    private readonly PluginConfig _config;
    private readonly ColourSchemes _schemes;
    private readonly ObstacleColorizerManager? _manager;
    private Color? _savedGlobalColour;

    private ObstacleColourReverter(PluginConfig config, ColourSchemes schemes, [InjectOptional] ObstacleColorizerManager? manager)
    {
        _config = config;
        _schemes = schemes;
        _manager = manager;
    }

    protected override bool IsAvailable => _manager != null;

    protected override bool ClearsChromaFor(ViewKind view) => _config.ChromaEventsDisabledFor(view);

    protected override bool UsesOwnColoursFor(ViewKind view) => _schemes.HasMapColours && _config.MapColoursDisabledFor(view);

    protected override Color? OwnColour(ObjectColorizer colouriser) => _schemes.Own!.obstaclesColor;

    protected override void CollectColourisers(List<ObjectColorizer> colourisers)
    {
        foreach (var pair in _manager!.Colorizers)
        {
            if (pair.Key.isActiveAndEnabled)
            {
                colourisers.Add(pair.Value);
            }
        }
    }

    protected override void ClearGlobalColour()
    {
        _savedGlobalColour = _manager!.GlobalColor;
        _manager.GlobalColor = null;
    }

    protected override void RestoreGlobalColour()
    {
        _manager!.GlobalColor = _savedGlobalColour;
    }
}
