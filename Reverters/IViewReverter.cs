using Chromason.Rendering;

namespace Chromason.Reverters;

internal interface IViewReverter
{
    bool ShouldApply(ViewKind view);

    /// <summary>Called before the camera culls: swap Chroma's state for the vanilla one.</summary>
    void Apply();

    /// <summary>Called after the camera rendered: put Chroma's state back exactly as it was.</summary>
    void Restore();
}
