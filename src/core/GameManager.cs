using Godot;
using SYNK33.chart;
using SYNK33.spawner;

namespace SYNK33.core;

public partial class GameManager : Node {
    [Export] public required Conductor Conductor;
    [Export] public required InputManager InputManager;
    [Export] public required JudgementManager JudgementManager;
    [Export] public required Spawner Spawner;
    public override void _Ready() {
        base._Ready();
    }
}