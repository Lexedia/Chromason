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

    private static System.Action<NoteController>? _setNoteColour;
    private static MethodInfo? _patchColours;
    private static FieldInfo? _patchedSchemeField;

    internal static (ColorManager ColourManager, ColorScheme? Own)? Level { get; set; }

    internal static bool Available => _setNoteColour != null;

    internal static void Initialise()
    {
#if BEAT_SABER_1_29_0
        // NoteTweaks doesnt support 1.29
        // n.b technically it does but im way too fucking lazy to backport the old plugin so live with it... sorry ⚞^. .^⚟
#elif BEAT_SABER_1_42_0
        // We dont do shit here too because it crashes the game otherwise and i dont really know why?
        // So like, my bad i guess
#else
        var hook = AccessTools.TypeByName("NoteTweaks.Hooks.SetGameNoteControllerColorHook");
        var colourTweaks = AccessTools.TypeByName("NoteTweaks.Patches.NoteColorTweaks");
        if (hook == null || colourTweaks == null)
        {
            return;
        }

        _setNoteColour = AccessTools.Method(hook, "SetNoteControllerColor", [typeof(NoteController)]) is { IsStatic: true } setNoteColour
            ? (System.Action<NoteController>)setNoteColour.CreateDelegate(typeof(System.Action<NoteController>))
            : null;
        _patchColours = AccessTools.Method(colourTweaks, "PatchColors", [typeof(ColorSchemeSO)]);
        _patchedSchemeField = AccessTools.Field(colourTweaks, "_patchedScheme") is { IsStatic: true } field ? field : null;

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
        catch (System.Exception exception)
        {
            Plugin.Log.Warn($"Could not get NoteTweaks' version of your colours, using them as they are: {exception}");
            _patchColours = null;
            return null;
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

        if (!Variants.TryGetValue(noteController, out var variants))
        {
            if (CreateVariants(noteController, level.Own != null) is not { } created)
            {
                return;
            }

            variants = created;
            Variants[noteController] = variants;
        }

        var colourType = noteController.noteData.colorType;
        var bodyAlpha = variants.Body.materialPropertyBlock.GetColor(ColourId).a;

        Read(variants.Controllers, variants.Saved);
        try
        {
            if (level.Own is { } ownScheme && variants.Own != null)
            {
                var ownColour = colourType == ColorType.ColorB ? ownScheme.saberBColor : ownScheme.saberAColor;
                Recolour(noteController, variants, ownColour.ColorWithAlpha(bodyAlpha), variants.Own);
            }

            Recolour(noteController, variants, level.ColourManager.ColorForType(colourType).ColorWithAlpha(bodyAlpha), variants.Scheme);
        }
        finally
        {
            Write(variants.Controllers, variants.Saved);
        }
    }

    private static NoteVariants? CreateVariants(NoteController noteController, bool withOwn)
    {
        var visuals = noteController.GetComponent<ColorNoteVisuals>();
        if (visuals == null || visuals.transform.childCount == 0)
        {
            return null;
        }

        var body = visuals.transform.GetChild(0).GetComponent<MaterialPropertyBlockController>();
        if (body == null || !HasColour(body))
        {
            return null;
        }

        var controllers = new List<MaterialPropertyBlockController>();
        foreach (var controller in noteController.GetComponentsInChildren<MaterialPropertyBlockController>(true))
        {
            if (HasColour(controller))
            {
                controllers.Add(controller);
            }
        }

        return new NoteVariants([.. controllers], body, withOwn);
    }

    private static bool HasColour(MaterialPropertyBlockController controller)
    {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_42_0
        return false;
#else
        return controller.materialPropertyBlock.HasColor(ColourId);
#endif
    }

    private static void Recolour(NoteController note, NoteVariants variants, Color colour, Color[] into)
    {
        variants.Body.materialPropertyBlock.SetColor(ColourId, colour);
        _setNoteColour!(note);
        Read(variants.Controllers, into);
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
        internal readonly MaterialPropertyBlockController Body;

        internal readonly Color[] Scheme;
        internal readonly Color[]? Own;

        internal readonly Color[] Saved;

        internal NoteVariants(MaterialPropertyBlockController[] controllers, MaterialPropertyBlockController body, bool withOwn)
        {
            Controllers = controllers;
            Body = body;
            Scheme = new Color[controllers.Length];
            Own = withOwn ? new Color[controllers.Length] : null;
            Saved = new Color[controllers.Length];
        }
    }
}
