using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SYNK33.chart;

namespace SYNK33.core;

public partial class JudgementManager : Node {
    [Signal]
    public delegate void NoteMissedEventHandler(NoteType type, int bar, int beat, double sixteenth);

    [Signal]
    public delegate void NoteHitEventHandler(NoteType type, int bar, int beat, double sixteenth, Judgement judgement);

    [Export] public required Conductor Conductor;
    [Export] public required InputManager InputManager;
    private readonly Queue<RhythmInput> _inputs = new();

    private readonly List<Note> _notes = [];

    public override void _Ready() {
        base._Ready();
        CopyNotes();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        Judge(InputManager.PopInput());
        CheckMissedNotes();
    }

    private void CopyNotes() {
        foreach (var note in Conductor.Chart.Notes) {
            _notes.Add(note.ToNote());
        }
    }

    private void Judge(RhythmInput? input) {
        if (input == null) return;
        var note = _notes.Find(it => it.Type == input.NoteType);
        if (note == null) return;
        var time = note.Time(Conductor.SecondsPerBeat, Conductor.Chart.BeatsPerMeasure) - input.Timestamp;
        var absoluteTime = Math.Abs(time);
        GD.Print(note + " " + time);
        Judgement? judgement = absoluteTime switch {
            <= JudgementTiming.Perfect => Judgement.Perfect,
            <= JudgementTiming.Great => Judgement.Great,
            <= JudgementTiming.Okay => Judgement.Okay,
            _ => null
        };
        if (!judgement.HasValue) return;
        GD.Print(judgement);
        _notes.Remove(note);
        EmitSignalNoteHit(note.Type, note.Bar, note.Beat, note.Sixteenth, judgement.Value);
    }

    private void CheckMissedNotes() {
        foreach (var note in _notes.ToList()) {
            var time = Conductor.SongPosition - note.Time(Conductor.SecondsPerBeat, Conductor.Chart.BeatsPerMeasure);
            if (!(time > JudgementTiming.Miss)) continue;
            GD.Print(time);
            _notes.Remove(note);
            EmitSignalNoteMissed(note.Type, note.StartTime.Bar, note.StartTime.Beat, note.Sixteenth);
            GD.Print("note missed " + note);
        }
    }

  
}

public static class Extensions {
    public static double Time(this Note note, float secondsPerBeat, int notesBerBar) {
        return (note.Bar * notesBerBar + note.Beat + note.Sixteenth / 4.0) * secondsPerBeat;
    }
}
public static class JudgementTiming {
    // public const double Perfect = 0.0165;
    // public const double Great = 0.033;
    // public const double Okay = 0.066;
    // public const double Miss = 0.099;
    public const double Perfect = 0.05;
    public const double Great = 0.08;
    public const double Okay = 0.11;
    public const double Miss = 0.130;
}

public enum Judgement {
    Perfect,
    Great,
    Okay,
    Miss,
}