extends CanvasLayer


var _transition_scene:PackedScene


@onready var animation_player:AnimationPlayer = $AnimationPlayer


func transition_to_packed(scene:PackedScene) -> void:
	_transition_scene = scene
	animation_player.play(&"transition")


func _transition_callback() -> void:
	# I don't likey this
	get_tree().change_scene_to_packed(_transition_scene)
