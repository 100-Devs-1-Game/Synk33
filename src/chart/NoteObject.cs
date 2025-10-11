using System;
using Godot;
using SYNK33.core;

namespace SYNK33.chart;

[GlobalClass]
public partial class NoteObject : Node2D {
    public float Speed;
    public required NoteTime StartTime;
    [Export] public NoteType Type;

    public override void _Ready() {
        SetSelfModulate(Type switch {
                NoteType.Left => new Color("f7000f"),
                NoteType.Middle => new Color("209f00"),
                NoteType.Right => new Color("008bd9"),
                _ => throw new ArgumentOutOfRangeException()
            }
        );
    }

    public override void _Process(double delta) {
        base._Process(delta);
        Position = Position with { Y = (float)(Position.Y + Speed * delta) };
    }

    public void SetMissed(NoteType type, int bar, int beat, double sixteenth) {
        if (!IsEventMatching(type, bar, beat, sixteenth)) return;
        SetSelfModulate(SelfModulate.Darkened(1f));
    }

    public void SetHit(NoteType type, int bar, int beat, double sixteenth, Judgement judgement) {
        if (!IsEventMatching(type, bar, beat, sixteenth)) return;
        SetSelfModulate(judgement switch {
            Judgement.Perfect => new Color(255, 255, 255),
            Judgement.Great => new Color(255, 255, 0),
            Judgement.Okay => new Color(255, 0, 2555),
            Judgement.Miss => new Color(0, 0, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(judgement), judgement, null)
        });
    }

    private bool IsEventMatching(NoteType type,  int bar, int beat, double sixteenth) {
        return IsEventMatching(type, new NoteTime(bar, beat, sixteenth));
    }
    private bool IsEventMatching(NoteType type, NoteTime time) {
        if (Type == type && StartTime == time) {
            GD.Print("matching " + time);
        }
        return Type == type && StartTime == time;
    }
}