extends Button

@export_enum("Main Menu","Settings","Song Select","Credits") var scene:int = 0


func _pressed() -> void:
	TransitionManager.play_transition()
	await TransitionManager.transition_midpoint
	SceneManager.ChangeSceneToBasicScene(scene)
