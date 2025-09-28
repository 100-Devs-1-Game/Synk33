using Godot;
using Chart = SYNK33.chart.Chart;

namespace SYNK33.conductor;

[GlobalClass]
public partial class Conductor : Node {
    public double SongPosition;
    [Export] public Chart Chart { get; set; }
    [Export] public AudioStreamPlayer Player { get; set; }
    [Export] public float Bpm { get; set; } = 120f;
    [Export] public float OffsetSeconds { get; set; }
    [Export] public float InputOffsetMs { get; set; }

    public float SecondsPerBeat => 60f / Bpm;

    public override void _Ready() {
        base._Ready();
        Player.Play();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        SongPosition = Player.GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix();
        SongPosition -= AudioServer.GetOutputLatency();
    }
}