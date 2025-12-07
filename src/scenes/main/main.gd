extends Node


func _ready() -> void:
	await get_tree().process_frame
	SceneManager.ChangeSceneToBasicScene(0);
	queue_free()
