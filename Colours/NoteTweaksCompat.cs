using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Chromason.Colours;

internal static class NoteTweaksCompat
{
    internal const string PatchedSchemeId = "NoteTweaksPatched";

    private static readonly int ColourId = Shader.PropertyToID("_Color");

    private static readonly Dictionary<NoteControllerBase, NoteVariants> Variants = [];

    private static MethodInfo? _setNoteColour;
    private static MethodInfo? _patchColours;
    private static FieldInfo? _patchedSchemeField;

    internal static (ColorManager ColourManager, ColorScheme? Own)? Level { get; set; }

    internal static bool Available => _setNoteColour != null;

    internal static void Initialise()
    {
#if BEAT_SABER_1_29_0
        // NoteTweaks doesnt support 1.29
#else
        var hook = AccessTools.TypeByName("NoteTweaks.Hooks.SetGameNoteControllerColorHook");
        var colourTweaks = AccessTools.TypeByName("NoteTweaks.Patches.NoteColorTweaks");
        if (hook == null || colourTweaks == null)
        {
            return;
        }

        _setNoteColour = AccessTools.Method(hook, "SetNoteControllerColor", [typeof(NoteController)]);
        _patchColours = AccessTools.Method(colourTweaks, "PatchColors");
        _patchedSchemeField = AccessTools.Field(colourTweaks, "_patchedScheme");

        if (_setNoteColour == null)
        {
            Plugin.Log.Warn("NoteTweaks found, but not a supported version: notes keep its colours in every view");
            return;
        }

        Plugin.Log.Debug("NoteTweaks found, note colours will follow it");
#endif
    }

    internal static ColorScheme? Patch(ColorScheme scheme)
    {
        if (_patchColours == null)
        {
            return null;
        }

        var schemeAsset = ScriptableObject.CreateInstance<ColorSchemeSO>();
        schemeAsset._colorScheme = scheme;

        var remembered = _patchedSchemeField?.GetValue(null);
        try
        {
            return _patchColours.Invoke(null, [schemeAsset]) as ColorScheme;
        }
        finally
        {
            _patchedSchemeField?.SetValue(null, remembered);
            Object.Destroy(schemeAsset);
        }
    }

    internal static bool TryGetVariants(NoteControllerBase note, out NoteVariants variants) =>
        Variants.TryGetValue(note, out variants);

    internal static void Clear()
    {
        Variants.Clear();
        Level = null;
    }

    internal static void BuildVariants(NoteController noteController)
    {
        if (Level is not { } level || noteController == null)
        {
            return;
        }

        var visuals = noteController.GetComponent<ColorNoteVisuals>();
        if (visuals == null || visuals.transform.childCount == 0)
        {
            return;
        }

        var body = visuals.transform.GetChild(0).GetComponent<MaterialPropertyBlockController>();
        if (body == null || !HasColour(body))
        {
            return;
        }

        var controllers = new List<MaterialPropertyBlockController>();
        foreach (var controller in noteController.GetComponentsInChildren<MaterialPropertyBlockController>(true))
        {
            if (HasColour(controller))
            {
                controllers.Add(controller);
            }
        }

        var colourType = noteController.noteData.colorType;
        var live = Read(controllers);
        var bodyAlpha = body.materialPropertyBlock.GetColor(ColourId).a;

        var own = level.Own is { } ownScheme
            ? Recolour(noteController, controllers, body, (colourType == ColorType.ColorB ? ownScheme.saberBColor : ownScheme.saberAColor).ColorWithAlpha(bodyAlpha))
            : null;
        var scheme = Recolour(noteController, controllers, body, level.ColourManager.ColorForType(colourType).ColorWithAlpha(bodyAlpha));

        Write(controllers, live);
        Variants[noteController] = new NoteVariants([.. controllers], scheme, own);
    }

    private static bool HasColour(MaterialPropertyBlockController controller)
    {
#if BEAT_SABER_1_29_0
        return false;
#else
        return controller.materialPropertyBlock.HasColor(ColourId);
#endif
    }

    private static Color[] Recolour(NoteController note, List<MaterialPropertyBlockController> controllers, MaterialPropertyBlockController body, Color colour)
    {
        body.materialPropertyBlock.SetColor(ColourId, colour);
        _setNoteColour!.Invoke(null, [note]);
        return Read(controllers);
    }

    private static Color[] Read(IReadOnlyList<MaterialPropertyBlockController> controllers)
    {
        var colours = new Color[controllers.Count];
        Read(controllers, colours);
        return colours;
    }

    internal static void Read(IReadOnlyList<MaterialPropertyBlockController> controllers, Color[] colours)
    {
        for (var i = 0; i < colours.Length; i++)
        {
            colours[i] = controllers[i].materialPropertyBlock.GetColor(ColourId);
        }
    }

    internal static void Write(IReadOnlyList<MaterialPropertyBlockController> controllers, Color[] colours)
    {
        for (var i = 0; i < colours.Length; i++)
        {
            var controller = controllers[i];
            if (controller == null)
            {
                continue;
            }

            controller.materialPropertyBlock.SetColor(ColourId, colours[i]);
            controller.ApplyChanges();
        }
    }

    internal readonly struct NoteVariants
    {
        internal readonly MaterialPropertyBlockController[] Controllers;

        internal readonly Color[] Scheme;
        internal readonly Color[]? Own;

        internal readonly Color[] Saved;

        internal NoteVariants(MaterialPropertyBlockController[] controllers, Color[] scheme, Color[]? own)
        {
            Controllers = controllers;
            Scheme = scheme;
            Own = own;
            Saved = new Color[controllers.Length];
        }
    }
}
