using Godot;

namespace SYNK33.chart;

[GlobalClass]
public partial class HoldNoteObject : NoteObject {
    public required NoteTime EndTime;
    public float PixelsPerBeat;
    public float JudgementY;
    public int BeatsPerMeasure;
    private TextureRect? _trail;
    private Sprite2D? _noteSprite;
    private bool _held;

    public override void _Ready() {
        base._Ready();
        _trail = GetNode<TextureRect>("TrailTexture");
        _noteSprite = GetNode<Sprite2D>("Texture");
        var beats = EndTime - StartTime;
        var pixelLength = (beats.Bar * BeatsPerMeasure + beats.Beat + beats.Sixteenth / 4.0f) * PixelsPerBeat;
        var holdSprite = GetNode<TextureRect>("TrailTexture");
        holdSprite.Size = new Vector2(holdSprite.Size.X, (float)pixelLength * 1 / Scale.Y);
    }

    public override void _Process(double delta) {
        base._Process(delta);

        if (_held) {
            _noteSprite.GlobalPosition = _noteSprite.GlobalPosition with { Y = JudgementY };
        }
    }

    public void StartHold(NoteType type, int bar, int beat, double sixteenth) {
        if (IsEventMatching(type, bar, beat, sixteenth)) {
            _held = true;
        }
    }

    public void EndHold(NoteType type, int bar, int beat, double sixteenth) {
        if (IsEventMatching(type, bar, beat, sixteenth)) {
            _held = false;
        }
    }
}