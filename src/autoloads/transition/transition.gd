extends CanvasLayer


const DEFAULT_ANIMATION: StringName = &"wipe"


## Emitted at the point in a transition when the screen is totally obscured.
signal transition_midpoint()
## Emitted at the end of a transition.
signal transition_endpoint()


var transitioning:bool


@onready var animation_player:AnimationPlayer = $AnimationPlayer


func _input(_event: InputEvent) -> void:
	if transitioning:
		get_viewport().set_input_as_handled()


func play_transition(animation_name:StringName = DEFAULT_ANIMATION) -> void:
	transitioning = true
	animation_player.play(animation_name)


func transition_to_file(path:String, animation_name:StringName = DEFAULT_ANIMATION) -> void:
	play_transition(animation_name)
	await transition_midpoint
	get_tree().change_scene_to_file(path)


## returns the midpoint time in a transition animation. Returns -1 if not applicable.
func get_transition_midpoint_time(animation_name:StringName) -> float:
	return _get_animation_method_callback_time(animation_name, ^".", &"_transition_midpoint_callback")

## returns the endpoint time in a transition animation. Returns -1 if not applicable.
func get_transition_endpoint_time(animation_name:StringName) -> float:
	return _get_animation_method_callback_time(animation_name, ^".", &"_transition_endpoint_callback")


func _get_animation_method_callback_time(animation_name:StringName, 
		node:NodePath, method:StringName) -> float:
	var animation := animation_player.get_animation(animation_name)
	if not animation:
		return -1
	
	var track_idx := animation.find_track(node, Animation.TYPE_METHOD)
	if track_idx == -1:
		push_error("Animation \"%s\" has no method track for node \"%s\"" % [animation_name, node])
		return -1
	
	for key_idx in animation.track_get_key_count(track_idx):
		var callback: Dictionary = animation.track_get_key_value(track_idx, key_idx)
		print(callback)
		if callback["method"] == method:
			print(animation.track_get_key_time(track_idx, key_idx))
			return animation.track_get_key_time(track_idx, key_idx)
	
	push_error("Animation \"%s\" has no method call for node \"%s\" method \"\"" % 
			[animation_name, node, method])
	return -1


func _transition_midpoint_callback() -> void:
	transition_midpoint.emit()


func _transition_endpoint_callback() -> void:
	transitioning = false
	transition_endpoint.emit()
