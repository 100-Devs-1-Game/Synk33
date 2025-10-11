extends Button

@export_file_path("*.tscn") var scene_path:String


func _pressed() -> void:
	Transition.transition_to_file(scene_path)
