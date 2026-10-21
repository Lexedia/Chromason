using System;
using System.Collections.Generic;
using Zenject;

namespace Chromason.Colours;

internal class NoteTweaksSpawnTracker : IInitializable, ILateTickable, IDisposable
{
    private readonly BeatmapObjectManager _beatmapObjectManager;
    private readonly List<NoteController> _spawned = [];

    private NoteTweaksSpawnTracker(BeatmapObjectManager beatmapObjectManager)
    {
        _beatmapObjectManager = beatmapObjectManager;
    }

    public void Initialize()
    {
        _beatmapObjectManager.noteWasSpawnedEvent += HandleNoteWasSpawned;
    }

    public void Dispose()
    {
        _beatmapObjectManager.noteWasSpawnedEvent -= HandleNoteWasSpawned;
    }

    public void LateTick()
    {
        if (_spawned.Count == 0)
        {
            return;
        }

        foreach (var note in _spawned)
        {
            try
            {
                NoteTweaksCompat.BuildVariants(note);
            }
            catch (Exception exception)
            {
                // rip bozo
                Plugin.Log.Error($"Could not follow NoteTweaks' colours, stopping for this level: {exception}");
                NoteTweaksCompat.Clear();
                break;
            }
        }

        _spawned.Clear();
    }

    private void HandleNoteWasSpawned(NoteController note)
    {
        if (NoteTweaksCompat.Level != null && note is GameNoteController or BurstSliderGameNoteController)
        {
            _spawned.Add(note);
        }
    }
}
