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

	private Node MainMenu = null!;
	private Node Settings = null!;
	private Node SongSelect = null!;
	private Node Credits = null!;

	public override void _Ready() {
		base._Ready();
		MainMenu = GetAndRemoveNode("MainMenu");
		Settings = GetAndRemoveNode("Settings");
		SongSelect = GetAndRemoveNode("SongSelect");
		Credits = GetAndRemoveNode("Credits");
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
        GD.Print($"Changing Scene to {newScene}...");
	    Node currentScene = GetTree().CurrentScene;
        currentScene.GetParent().RemoveChild(currentScene);
		GetTree().Root.AddChild(newScene);
		GetTree().CurrentScene = newScene;
	}
	public void ChangeSceneToGame(Chart chart) {
		// TODO: Implement this
	}
	public void ChangeSceneToResults() {
		// TODO: Implement this
	}
	private Node GetAndRemoveNode(NodePath path){
		Node node = GetNode(path);
		RemoveChild(node);
		return node;
	}
	private N GetAndRemoveNode<N>(NodePath path) where N : Node{
		N node = GetNode<N>(path);
		RemoveChild(node);
		return node;
	}
}
