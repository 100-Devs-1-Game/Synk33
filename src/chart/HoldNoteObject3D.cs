using Godot;

namespace SYNK33.chart;

[GlobalClass]
public partial class HoldNoteObject3D : NoteObject3D {
    public required NoteTime EndTime;
    public long BeatsPerMeasure;
    private MeshInstance3D? _trailMesh;
    private bool _held;

    public float UnitsPerBeat = 30f;

    public override void _Ready() {
        base._Ready();
        var trailNode = GetNodeOrNull<Node3D>("Trail");
        GetNode<Node3D>("Trail/trail_purple").Visible = Type == NoteType.Left;
        GetNode<Node3D>("Trail/trail_blue").Visible = Type == NoteType.Middle;
        GetNode<Node3D>("Trail/trail_orange").Visible = Type == NoteType.Right;

        var beats = EndTime - StartTime;
        var beatCount = (beats.Bar * BeatsPerMeasure + beats.Beat + beats.Sixteenth / 4.0f);

        var worldLength = (float)(beatCount * UnitsPerBeat);

        if (trailNode != null) {
            var originalScale = trailNode.Scale;
            trailNode.Scale = new Vector3(worldLength / originalScale.X, originalScale.Y, originalScale.Z);
        }
    }

    public void StartHold(NoteType type, long bar, long beat, double sixteenth) {
        if (IsEventMatching(type, bar, beat, sixteenth)) {
            _held = true;
        }
    }

    public void EndHold(NoteType type, long bar, long beat, double sixteenth) {
        if (IsEventMatching(type, bar, beat, sixteenth)) {
            _held = false;
        }
    }
}
