using System;
using Godot;
using SYNK33.chart;
using SYNK33.core;

namespace SYNK33.spawner;

public partial class Spawner : Node2D {
    private Chart _chart;

    private Conductor _conductor;
    [Export] public float ScrollSpeed { get; set; } = 1.0f;
    [Export] public float AudioOffset { get; set; }

    [Export] public NodePath JudgementLine { get; set; }

    [Export] public NodePath Conductor { get; set; }
    [Export] public JudgementManager JudgementManager { get; set; }
    

    public override void _Ready() {
        _conductor = GetNode<Conductor>(Conductor);
        _chart = _conductor.Chart;
        SpawnNotes();
    }

    private void SpawnNotes() {
        var judgementY = GetNode<Marker2D>(JudgementLine).GlobalPosition.Y;
        foreach (var note in _chart.Notes) {
            var absoluteBeat = note.Bar * _chart.BeatsPerMeasure + note.Beat + (float)note.Sixteenth/ 4.0f + AudioOffset;
            var spawnY = absoluteBeat * ScrollSpeed * judgementY;
            SpawnNote(note.ToNote(), new Vector2(960f, -spawnY));
        }
    }

    private void SpawnNote(Note note, Vector2 position) {
        var judgementY = GetNode<Marker2D>(JudgementLine).GlobalPosition.Y;
        var noteInstance = GD.Load<PackedScene>("res://chart/NoteObject.tscn").Instantiate<NoteObject>();
        noteInstance.Speed = judgementY / _conductor.SecondsPerBeat * ScrollSpeed;
        noteInstance.Type = note.Type;
        var lanePosition = note.Type switch {
            NoteType.Left => position.X - 100,
            NoteType.Middle => position.X,
            NoteType.Right => position.X + 100,
            _ => throw new ArgumentOutOfRangeException()
        };
        noteInstance.Position = new Vector2(lanePosition, judgementY + position.Y);
        noteInstance.StartTime = note.StartTime;
        JudgementManager.NoteMissed += noteInstance.SetMissed;
        JudgementManager.NoteHit += noteInstance.SetHit;
        AddChild(noteInstance);
    }
}