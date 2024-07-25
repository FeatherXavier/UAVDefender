using SixLabors.ImageSharp;

namespace YoloBackend.Scorer;

/// <summary>
/// Object prediction.
/// </summary>
public record YoloPrediction(YoloLabel Label, float Score, RectangleF Rectangle);
