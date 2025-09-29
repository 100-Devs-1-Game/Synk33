using Godot;

namespace SYNK33.chart;

// Temporary class until editor is implemented
[GlobalClass]
public partial class DebugNote : Resource {
    [Export] public float Beat { get; set; }
    [Export] public NoteType Type { get; set; }
    [Export] public float EndBeat { get; set; }

    public Note ToNote() {
        return EndBeat switch {
            0 => new Note.Tap(Beat, Type),
            _ => new Note.Hold(Beat, EndBeat, Type)
        };
    }
}