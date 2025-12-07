using Godot;
using SYNK33.chart;
using System;

namespace SYNK33.scenemanager;

public enum BasicScene {
	MainMenu,
	Settings,
	SongSelect,
	Credits,
}

public partial class SceneManager : Node {
	private static PackedScene MainMenuPacked = ResourceLoader.Load<PackedScene>("uid://dnk0thv8iasnk");
	private static PackedScene SettingsPacked = ResourceLoader.Load<PackedScene>("uid://bwpjtqjpf7sp0");
	private static PackedScene SongSelectPacked = ResourceLoader.Load<PackedScene>("uid://c75ew37wbtpa2");
	private static PackedScene CreditsPacked = ResourceLoader.Load<PackedScene>("uid://ovi7mj6ghtqq");
	private bool isCurrentSceneForeign = true;
	
	private Node MainMenu = null!;
	private Node Settings = null!;
	private Node SongSelect = null!;
	private Node Credits = null!;

	public override void _Ready() {
		base._Ready();
		MainMenu = MainMenuPacked.Instantiate();
		Settings = SettingsPacked.Instantiate();
		SongSelect = SongSelectPacked.Instantiate();
		Credits = CreditsPacked.Instantiate();
	}
	/// <summary>
	/// Switch to a basic scene that requires no arguments 
	/// </summary>
	/// <param name="scene"></param>
	public void ChangeSceneToBasicScene(BasicScene scene) {
		Node newScene = null!;
		switch (scene) {
			case BasicScene.MainMenu:
				newScene = MainMenu;
				break;
			case BasicScene.Settings:
				newScene = Settings;
				break;
			case BasicScene.SongSelect:
				newScene = SongSelect;
				break;
			case BasicScene.Credits:
				newScene = Credits;
				break;
		}
		SceneSwap(newScene);
		isCurrentSceneForeign = false;
	}
	public void ChangeSceneToForeignScene(PackedScene scene) {
		Node newScene = scene.Instantiate();
		SceneSwap(newScene);
		isCurrentSceneForeign = true;
	}
	public void ChangeSceneToGame(Chart chart) {
		// TODO: Implement this
	}
	public void ChangeSceneToResults() {
		// TODO: Implement this
	}
	private void SceneSwap(Node newScene) {
		Node currentScene = GetTree().CurrentScene;
		currentScene.GetParent().RemoveChild(currentScene);
		if (isCurrentSceneForeign) {
			currentScene.QueueFree();
		}
		GetTree().Root.AddChild(newScene);
		GetTree().CurrentScene = newScene;
	}
}
