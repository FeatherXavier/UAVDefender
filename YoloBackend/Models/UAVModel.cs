using YoloBackend.Scorer.Models.Abstract;

namespace YoloBackend.Scorer.Models;

public record UAVModel() : YoloModel(
    640,
    640,
    3,

    6,

    new[] { 8, 16, 32 },

    new[]
    {
        new[] { new[] { 010, 13 }, new[] { 016, 030 }, new[] { 033, 023 } },
        new[] { new[] { 030, 61 }, new[] { 062, 045 }, new[] { 059, 119 } },
        new[] { new[] { 116, 90 }, new[] { 156, 198 }, new[] { 373, 326 } }
    },

    new[] { 80, 40, 20 },

    0.20f,
    0.25f,
    0.45f,

    new[] { "output0" },

    new()
    {
        new(0,"UAV")
    },

    true
);
