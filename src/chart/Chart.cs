using Godot;
using Godot.Collections;

namespace SYNK33.chart;

[GlobalClass]
public partial class Chart : Resource {
    [ExportGroup("Info")] [Export] public string Designer { get; set; }

    [Export] public Difficulty Difficulty { get; set; }
    [Export] public int Level { get; set; }
    [Export] public float Bpm { get; set; }
    [Export(PropertyHint.Range, "1,32,1")] public int BeatsPerMeasure { get; set; } = 4;

    [ExportGroup("Music")] [Export] public Array<DebugNote> Notes { get; set; }
}

public enum Difficulty {
    Easy,
    Normal,
    Hard,
    Expert
}