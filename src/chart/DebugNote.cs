using System;
using Godot;

namespace SYNK33.chart;

// Temporary class until editor is implemented
[GlobalClass]
public partial class DebugNote : Resource {
    [Export] public int Bar { get; set; }
    [Export] public int Beat { get; set; }
    [Export] public double Sixteenth { get; set; }
    [Export] public NoteType Type { get; set; }

    public Note ToNote() {
        return new Note.Tap(new NoteTime(Bar, Beat, Sixteenth), Type);
    }
}