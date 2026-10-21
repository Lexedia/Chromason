using System.Collections.Generic;
using Chroma.Colorizer;
using Chromason.Configuration;
using Chromason.Rendering;
using UnityEngine;
using Zenject;

namespace Chromason.Reverters;

internal class BombColourReverter : ObjectColouriserReverter
{
    private readonly PluginConfig _config;
    private readonly BombColorizerManager? _manager;
    private Color? _savedGlobalColour;

    private BombColourReverter(PluginConfig config, [InjectOptional] BombColorizerManager? manager)
    {
        _config = config;
        _manager = manager;
    }

    protected override bool IsAvailable => _manager != null;

    protected override bool ClearsChromaFor(ViewKind view) => _config.ChromaEventsDisabledFor(view);

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
