using Godot;
using SYNK33.chart;
using SYNK33.conductor;

namespace SYNK33.spawner;

public partial class Spawner : Node2D {
    private Chart _chart;

    private Conductor _conductor;
    [Export] public float ScrollSpeed { get; set; } = 1.0f;
    [Export] public float AudioOffset { get; set; }

    [Export] public NodePath JudgementLine { get; set; }

    [Export] public NodePath Conductor { get; set; }

    public override void _Ready() {
        _conductor = GetNode<Conductor>(Conductor);
        _chart = _conductor.Chart;
        SpawnNotes();
    }

    private void SpawnNotes() {
        var judgementY = GetNode<Marker2D>(JudgementLine).Position.Y;
        foreach (var note in _chart.Notes) {
            var absoluteBeat = note.Beat * _chart.BeatsPerMeasure + AudioOffset;
            var spawnY = absoluteBeat * ScrollSpeed * judgementY / _chart.BeatsPerMeasure;
            SpawnNote(note.ToNote(), new Vector2(960f, -spawnY));
        }
    }

    private void SpawnNote(Note note, Vector2 position) {
        var judgementY = GetNode<Marker2D>(JudgementLine).Position.Y;
        var noteInstance = GD.Load<PackedScene>("res://chart/NoteObject.tscn").Instantiate<NoteObject>();
        noteInstance.Speed = judgementY / _conductor.SecondsPerBeat * ScrollSpeed;
        noteInstance.Type = note.Type;
        noteInstance.Position = new Vector2(position.X, judgementY + position.Y);
        AddChild(noteInstance);
    }
}