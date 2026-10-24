using System;
using System.Collections.Generic;
using Chroma.Colorizer;
using Chromason.Colours;
using Chromason.Configuration;
using Chromason.Rendering;
using UnityEngine;
using Zenject;

namespace Chromason.Reverters;

internal class VanillaMapColourReverter : IViewReverter, IInitializable, IDisposable
{
    private static readonly int ColourId = Shader.PropertyToID("_Color");

    private readonly PluginConfig _config;
    private readonly ColourSchemes _schemes;
    private readonly BeatmapObjectManager _beatmapObjectManager;
    private readonly bool _chromaColours;

    private readonly List<(NoteController Note, ColorNoteVisuals Visuals)> _notes = [];
    private readonly List<SliderController> _arcs = [];
    private readonly List<ObstacleController> _obstacles = [];

    private readonly List<(MaterialPropertyBlockController Controller, Color Colour)> _saved = [];
    private readonly List<NoteTweaksCompat.NoteVariants> _swappedNotes = [];
    private readonly List<ObstacleController> _swappedObstacles = [];

    private bool _tracking;

    private VanillaMapColourReverter(
        PluginConfig config,
        ColourSchemes schemes,
        BeatmapObjectManager beatmapObjectManager,
        [InjectOptional] NoteColorizerManager? chromaNotes)
    {
        _config = config;
        _schemes = schemes;
        _beatmapObjectManager = beatmapObjectManager;
        _chromaColours = chromaNotes != null;
    }

    public void Initialize()
    {
        if (_chromaColours || !_schemes.HasMapColours)
        {
            return;
        }

        _tracking = true;
        _beatmapObjectManager.noteWasSpawnedEvent += HandleNoteWasSpawned;
        _beatmapObjectManager.noteWasDespawnedEvent += HandleNoteWasDespawned;
        _beatmapObjectManager.sliderWasSpawnedEvent += _arcs.Add;
        _beatmapObjectManager.sliderWasDespawnedEvent += HandleArcWasDespawned;
        _beatmapObjectManager.obstacleWasSpawnedEvent += _obstacles.Add;
        _beatmapObjectManager.obstacleWasDespawnedEvent += HandleObstacleWasDespawned;
    }

    public void Dispose()
    {
        if (!_tracking)
        {
            return;
        }

        _beatmapObjectManager.noteWasSpawnedEvent -= HandleNoteWasSpawned;
        _beatmapObjectManager.noteWasDespawnedEvent -= HandleNoteWasDespawned;
        _beatmapObjectManager.sliderWasSpawnedEvent -= _arcs.Add;
        _beatmapObjectManager.sliderWasDespawnedEvent -= HandleArcWasDespawned;
        _beatmapObjectManager.obstacleWasSpawnedEvent -= _obstacles.Add;
        _beatmapObjectManager.obstacleWasDespawnedEvent -= HandleObstacleWasDespawned;
    }

    public void Apply(ViewKind view)
    {
        if (!_tracking || !_config.MapColoursDisabledFor(view))
        {
            return;
        }

        var own = _schemes.Own!;
        ApplyNotes(own);
        ApplyArcs(own);
        ApplyObstacles(own);
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

        foreach (var variants in _swappedNotes)
        {
            NoteTweaksCompat.Write(variants.Controllers, variants.Saved);
        }

        foreach (var obstacle in _swappedObstacles)
        {
            if (obstacle != null)
            {
                WriteObstacle(obstacle._stretchableObstacle, obstacle.color);
            }
        }

        _saved.Clear();
        _swappedNotes.Clear();
        _swappedObstacles.Clear();
    }

    private void ApplyNotes(ColorScheme own)
    {
        foreach (var (note, visuals) in _notes)
        {
            if (note == null || !note.isActiveAndEnabled || OwnColour(own, note.noteData.colorType) is not { } colour)
            {
                continue;
            }

            if (NoteTweaksCompat.TryGetVariants(note, out var variants))
            {
                if (variants.Own != null)
                {
                    NoteTweaksCompat.Read(variants.Controllers, variants.Saved);
                    _swappedNotes.Add(variants);
                    NoteTweaksCompat.Write(variants.Controllers, variants.Own);
                }

                continue;
            }

            if (visuals._noteColor == colour)
            {
                continue;
            }

            foreach (var controller in visuals._materialPropertyBlockControllers)
            {
                var saved = controller.materialPropertyBlock.GetColor(ColourId);
                _saved.Add((controller, saved));
                Write(controller, colour.ColorWithAlpha(saved.a));
            }
        }
    }

    private void ApplyArcs(ColorScheme own)
    {
        foreach (var arc in _arcs)
        {
            if (arc == null || !arc.isActiveAndEnabled || OwnColour(own, arc.sliderData.colorType) is not { } colour || colour == arc._initColor)
            {
                continue;
            }

            var controller = arc._materialPropertyBlockController;
            _saved.Add((controller, controller.materialPropertyBlock.GetColor(ColourId)));
            Write(controller, colour * arc.sliderIntensityEffect.colorIntensity);
        }
    }

    private void ApplyObstacles(ColorScheme own)
    {
        var colour = own.obstaclesColor;
        foreach (var obstacle in _obstacles)
        {
            if (obstacle == null || !obstacle.isActiveAndEnabled || obstacle.color == colour)
            {
                continue;
            }

            _swappedObstacles.Add(obstacle);
            WriteObstacle(obstacle._stretchableObstacle, colour);
        }
    }

    private static Color? OwnColour(ColorScheme own, ColorType type) => type switch
    {
        ColorType.ColorA => own.saberAColor,
        ColorType.ColorB => own.saberBColor,
        _ => null
    };

    private static void Write(MaterialPropertyBlockController controller, Color colour)
    {
        controller.materialPropertyBlock.SetColor(ColourId, colour);
        controller.ApplyChanges();
    }

    private static void WriteObstacle(StretchableObstacle obstacle, Color colour)
    {
        obstacle._obstacleFrame.color = colour;
        obstacle._obstacleFrame.Refresh();

        if (obstacle._obstacleFakeGlow != null)
        {
            obstacle._obstacleFakeGlow.color = colour;
            obstacle._obstacleFakeGlow.Refresh();
        }

        var addColour = colour * obstacle._addColorMultiplier;
        addColour.a = 0f;
        var tint = Color.Lerp(colour, Color.white, obstacle._obstacleCoreLerpToWhiteFactor);
        foreach (var controller in obstacle._materialPropertyBlockControllers)
        {
#if !BEAT_SABER_1_29_0
            controller.materialPropertyBlock.SetColor(StretchableObstacle._mainColorID, colour);
#endif
            controller.materialPropertyBlock.SetColor(StretchableObstacle._addColorID, addColour);
            controller.materialPropertyBlock.SetColor(StretchableObstacle._tintColorID, tint);
            controller.ApplyChanges();
        }
    }

    private void HandleNoteWasSpawned(NoteController note)
    {
        if (note is GameNoteController or BurstSliderGameNoteController && note.GetComponent<ColorNoteVisuals>() is { } visuals)
        {
            _notes.Add((note, visuals));
        }
    }

    private void HandleNoteWasDespawned(NoteController note)
    {
        _notes.RemoveAll(entry => entry.Note == note);
    }

    private void HandleArcWasDespawned(SliderController arc)
    {
        _arcs.Remove(arc);
    }

    private void HandleObstacleWasDespawned(ObstacleController obstacle)
    {
        _obstacles.Remove(obstacle);
    }
}
