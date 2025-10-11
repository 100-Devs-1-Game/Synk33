extends CanvasLayer


signal transition_midpoint()


@onready var animation_player:AnimationPlayer = $AnimationPlayer


func transition(method:Callable) -> void:
	animation_player.play(&"transition")
	transition_midpoint.connect(method, CONNECT_ONE_SHOT)


func _transition_callback() -> void:
	transition_midpoint.emit()
