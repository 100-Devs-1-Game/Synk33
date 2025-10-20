﻿using System;
using Godot;

namespace SYNK33.chart;

// Class for interoperability with Godot Editor
// For use in editor and signals
[GlobalClass]
public partial class GodotNote : Resource {
    public GodotNote() { }
    public GodotNote(Note note) {
        Bar =  note.Bar;
        Beat =  note.Beat;
        Sixteenth = note.Sixteenth;
        Type = note.Type;
        if (note is not Note.Hold) return;
        EndBar = note.Bar;
        EndBeat = note.Beat;
        EndSixteenth = note.Sixteenth;
    }
    [Export] public int Bar { get; set; }
    [Export] public int Beat { get; set; }
    [Export] public double Sixteenth { get; set; }
    [Export] public NoteType Type { get; set; }
    [Export] public int EndBar { get; set; }
    [Export] public int EndBeat { get; set; }
    [Export] public double EndSixteenth { get; set; }

    public Note ToNote() {
        return EndBar != 0
            ? new Note.Hold(new NoteTime(Bar, Beat, Sixteenth), new NoteTime(EndBar, EndBeat, EndSixteenth), Type)
            : new Note.Tap(new NoteTime(Bar, Beat, Sixteenth), Type);
    }
}