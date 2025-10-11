using System;
using Godot;

namespace SYNK33.chart;

public enum NoteType {
    Left,
    Middle,
    Right
}

public abstract record Note(NoteTime StartTime, NoteType Type) {
    public readonly int Bar = StartTime.Bar;
    public readonly int Beat = StartTime.Beat;
    public readonly double Sixteenth = StartTime.Sixteenth;
    public sealed record Tap(NoteTime StartTime, NoteType Type) : Note(StartTime, Type);

    public sealed record Hold(NoteTime StartTime, NoteTime EndNote, NoteType Type) : Note(StartTime, Type);
}

public record NoteTime(int Bar, int Beat, double Sixteenth) {
    public readonly int Bar = Bar;
    public readonly int Beat = Beat;
    public readonly double Sixteenth = Sixteenth;
}