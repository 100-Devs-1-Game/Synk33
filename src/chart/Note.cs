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

    public static NoteTime operator +(NoteTime left, NoteTime right) {
        return new NoteTime(left.Bar + right.Bar, left.Beat + right.Beat, left.Sixteenth + right.Sixteenth);
    }
    
    public static NoteTime operator -(NoteTime left, NoteTime right) {
        return new NoteTime(left.Bar - right.Bar, left.Beat - right.Beat, left.Sixteenth - right.Sixteenth);
    }

    public float ToMilliseconds(int notesBerBar, double secondsPerBeat) {
        return (float)((Bar * notesBerBar + Beat + Sixteenth / 4.0) * secondsPerBeat);
    }
}
