using Godot;
using Godot.Collections;

namespace SYNK33.chart;

[GlobalClass]
public partial class Chart : Resource {
    [ExportGroup("Info")] [Export] public required string Designer { get; set; }
    [Export] public long Level { get; set; }
    [Export(PropertyHint.Range, "0.1,2,0.1,or_greater")] public float TempoModifier { get; set; } = 1f;
    [Export(PropertyHint.Range, "1,32,1")] public long BeatsPerMeasure { get; set; } = 4;

    [Export] public required Song Song { get; set; }

    [ExportGroup("Music")] 
    [Export] public Array<GodotNote> Notes { get; set; }

    public float Bpm => Song.Bpm * TempoModifier;
}


