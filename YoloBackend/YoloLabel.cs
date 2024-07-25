using SixLabors.ImageSharp;

namespace YoloBackend.Scorer;

/// <summary>
/// Label of detected object.
/// </summary>
public record YoloLabel(int Id, string Name, Color Color, YoloLabelKind Kind)
{
    public YoloLabel(int id, string name) : this(id, name, Color.Yellow, YoloLabelKind.Generic) { }
}
