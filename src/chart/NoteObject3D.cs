using Godot;
using SYNK33.core;

namespace SYNK33.chart;

[GlobalClass]
public partial class NoteObject3D : Node3D {
	public float Speed = 1;
	public required NoteTime StartTime;
	[Export] public NoteType Type;

	public override void _Ready() {
		GetNode<Node3D>("note_purple").Visible = Type == NoteType.Left;
		GetNode<Node3D>("note_blue").Visible = Type == NoteType.Middle;
		GetNode<Node3D>("note_orange").Visible = Type == NoteType.Right;
		GetNode<Node3D>("note_black").Visible = false;
	}

	public override void _Process(double delta) {
		base._Process(delta);
		Position = Position with { Z = (float)(Position.Z + Speed * delta) };
	}

	public void SetMissed(NoteType type, long bar, long beat, double sixteenth) {
		if (!IsEventMatching(type, bar, beat, sixteenth)) return;
		GetNode<Node3D>("note_blue").Visible = false;
		GetNode<Node3D>("note_orange").Visible = false;
		GetNode<Node3D>("note_purple").Visible = false;
		GetNode<Node3D>("note_black").Visible = true;
	}

	public void SetHit(NoteType type, long bar, long beat, double sixteenth, Judgement judgement) {
		if (!IsEventMatching(type, bar, beat, sixteenth)) return;
		var label = GetNode<Label3D>("Score");
		label.Text = judgement.ToString();
	}

	protected bool IsEventMatching(NoteType type, long bar, long beat, double sixteenth) {
		return IsEventMatching(type, new NoteTime(bar, beat, sixteenth));
	}
	protected bool IsEventMatching(NoteType type, NoteTime time) {
		return Type == type && StartTime == time;
	}
}
