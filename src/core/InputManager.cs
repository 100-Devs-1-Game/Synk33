using System.Collections.Generic;
using Godot;
using SYNK33.chart;

namespace SYNK33.core;

public partial class InputManager : Node {
    [Export] public double SongStartTime { get; set; } = 0;
    private readonly Queue<RhythmInput> _inputs = new();

    public override void _Ready() {
        base._Ready();
        SongStartTime = Time.GetUnixTimeFromSystem();
    }

    public override void _Input(InputEvent @event) {
        if (@event is not InputEventKey keyEvent) return;
        var noteType = keyEvent switch {
            _ when @event.IsAction("left") => NoteType.Left,
            _ when @event.IsAction("middle") => NoteType.Middle,
            _ when @event.IsAction("right") => NoteType.Right,
            _ => (NoteType?)null
        };

        if (noteType.HasValue) {
            AddInput(noteType.Value, @event.IsPressed());
        }
    }

    public RhythmInput? PopInput() {
        return _inputs.Count > 0 ? _inputs.Dequeue() : null;
    }

    private void AddInput(NoteType type, bool pressed) {
        var timestamp = Time.GetUnixTimeFromSystem();
        var hit = new RhythmInput(type, pressed, timestamp - SongStartTime);
        _inputs.Enqueue(hit);
        // GD.Print(hit);
    }
}

public record RhythmInput(
    NoteType NoteType,
    bool Pressed,
    double Timestamp
);