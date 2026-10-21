using System;

namespace Chromason.Colours;

internal class ColourSchemes : IDisposable
{
    private ColourSchemes(ColorManager colourManager)
    {
        var level = colourManager._colorScheme;
        Own = OwnColourSchemeCapture.Captured;

        var noteTweaks = level?.colorSchemeId == NoteTweaksCompat.PatchedSchemeId && NoteTweaksCompat.Available;
        if (noteTweaks && Own != null)
        {
            Own = NoteTweaksCompat.Patch(Own) ?? Own;
        }

        HasMapColours = Own != null && level != null && !SameColours(Own, level);

        if (noteTweaks)
        {
            NoteTweaksCompat.Level = (colourManager, HasMapColours ? Own : null);
        }

        Plugin.Log.Debug($"Colours: level uses {level?.colorSchemeId}, own is {OwnColourSchemeCapture.Captured?.colorSchemeId}, NoteTweaks: {noteTweaks}, map colours: {HasMapColours}");
    }

    internal ColorScheme? Own { get; }

    internal bool HasMapColours { get; }

    public void Dispose()
    {
        NoteTweaksCompat.Clear();
    }

    private static bool SameColours(ColorScheme a, ColorScheme b) =>
        a.saberAColor == b.saberAColor &&
        a.saberBColor == b.saberBColor &&
        a.obstaclesColor == b.obstaclesColor &&
        a.environmentColor0 == b.environmentColor0 &&
        a.environmentColor1 == b.environmentColor1 &&
        a.environmentColorW == b.environmentColorW &&
        a.environmentColor0Boost == b.environmentColor0Boost &&
        a.environmentColor1Boost == b.environmentColor1Boost &&
        a.environmentColorWBoost == b.environmentColorWBoost;
}
