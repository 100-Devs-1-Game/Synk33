using System.Collections.Generic;
using Godot;

namespace SYNK33;

public partial class InputManager : Node {
    private readonly Queue<RhythmInput> _inputs = new();

    public override void _Input(InputEvent @event) {
        if (@event is not InputEventKey { Pressed: true } keyEvent) return;
        var timestamp = Time.GetUnixTimeFromSystem();
        var hit = new RhythmInput(keyEvent.Keycode, timestamp);
        _inputs.Enqueue(hit);
        GD.Print(hit);
    }
}

public record RhythmInput(
    Key Key,
    double Timestamp
);