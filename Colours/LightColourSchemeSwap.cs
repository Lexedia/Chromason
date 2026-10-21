using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chromason.Colours;

internal static class LightColourSchemeSwap
{
    private static readonly List<(SimpleColorSO Asset, Color Colour)> SavedAssets = [];

    internal static SchemeSwapScope Begin(LightSwitchEventEffect effect, ColorScheme scheme)
    {
        var scope = new SchemeSwapScope(SavedAssets.Count, effect._colorManager);
        var boost = scheme.supportsEnvironmentColorBoost;

        Swap(effect._lightColor0, scheme.environmentColor0);
        Swap(effect._highlightColor0, scheme.environmentColor0);
        Swap(effect._lightColor1, scheme.environmentColor1);
        Swap(effect._highlightColor1, scheme.environmentColor1);
        Swap(effect._lightColor0Boost, boost ? scheme.environmentColor0Boost : scheme.environmentColor0);
        Swap(effect._highlightColor0Boost, boost ? scheme.environmentColor0Boost : scheme.environmentColor0);
        Swap(effect._lightColor1Boost, boost ? scheme.environmentColor1Boost : scheme.environmentColor1);
        Swap(effect._highlightColor1Boost, boost ? scheme.environmentColor1Boost : scheme.environmentColor1);

        if (effect._colorManager != null)
        {
            effect._colorManager._colorScheme = scheme;
        }

        return scope;
    }

    private static void Swap(ColorSO colour, Color target)
    {
        var asset = colour switch
        {
            MultipliedColorSO multiplied => multiplied._baseColor,
            SimpleColorSO simple => simple,
            _ => null
        };

        if (asset == null)
        {
            return;
        }

        SavedAssets.Add((asset, asset.color));
        asset.SetColor(target);
    }

    internal readonly struct SchemeSwapScope : IDisposable
    {
        private readonly int _assetMark;
        private readonly ColorManager? _colourManager;
        private readonly ColorScheme? _previousScheme;

        internal SchemeSwapScope(int assetMark, ColorManager? colourManager)
        {
            _assetMark = assetMark;
            _colourManager = colourManager;
            _previousScheme = colourManager?._colorScheme;
        }

        public void Dispose()
        {
            for (var i = SavedAssets.Count - 1; i >= _assetMark; i--)
            {
                SavedAssets[i].Asset.SetColor(SavedAssets[i].Colour);
            }

            if (SavedAssets.Count > _assetMark)
            {
                SavedAssets.RemoveRange(_assetMark, SavedAssets.Count - _assetMark);
            }

            if (_colourManager != null)
            {
                _colourManager._colorScheme = _previousScheme;
            }
        }
    }
}
