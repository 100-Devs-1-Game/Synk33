extends CanvasLayer


signal transition_midpoint()
signal transition_endpoint()


var transitioning:bool


@onready var animation_player:AnimationPlayer = $AnimationPlayer


func _input(_event: InputEvent) -> void:
	if transitioning:
		get_viewport().set_input_as_handled()


func transition() -> void:
	transitioning = true
	animation_player.play(&"transition")
	await transition_midpoint


func transition_to_file(path:String) -> void:
	await transition()
	get_tree().change_scene_to_file.bind(path)


func _transition_midpoint_callback() -> void:
	transition_midpoint.emit()


func _transition_endpoint_callback() -> void:
	transitioning = false
	transition_endpoint.emit()
