extends CanvasLayer


signal transition_midpoint()


var transitioning:bool


@onready var animation_player:AnimationPlayer = $AnimationPlayer


func _input(_event: InputEvent) -> void:
	if transitioning:
		get_viewport().set_input_as_handled()


func transition(method:Callable) -> void:
	transitioning = true
	animation_player.play(&"transition")
	transition_midpoint.connect(method, CONNECT_ONE_SHOT)


func transition_to_file(path:String) -> void:
	transition(get_tree().change_scene_to_file.bind(path))


func _transition_midpoint_callback() -> void:
	transition_midpoint.emit()


func _transition_endpoint_callback() -> void:
	transitioning = false
