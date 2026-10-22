using Chromason.Rendering;

namespace Chromason.Reverters;

internal interface IViewReverter
{
    /// <summary>
    /// Called before a camera culls: swaps Chroma's state for what <paramref name="view"/> should show, if anything,
    /// and remembers what it changed.
    /// </summary>
    void Apply(ViewKind view);

    /// <summary>
    /// Called after the camera rendered: puts back exactly what the last <see cref="Apply"/> changed, and nothing if it
    /// changed nothing. Must not depend on anything decided outside <see cref="Apply"/>.
    /// </summary>
    void Restore();
}
