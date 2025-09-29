using System;
using Godot;

namespace SYNK33.chart;

[GlobalClass]
public partial class NoteObject : Node2D {
    public float Speed;
    public double Time;
    public NoteType Type;

    public override void _Ready() {
        SelfModulate = Type switch {
            NoteType.Left => new Color(255, 0, 0),
            NoteType.Middle => new Color(0, 255, 0),
            NoteType.Right => new Color(0, 0, 255),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override void _Process(double delta) {
        base._Process(delta);
        Position = Position with { Y = (float)(Position.Y + Speed * delta) };
    }
}