extends Button

@export var dialogue:DialogueResource

func _pressed() -> void:
	$"../../Dialogue".start(dialogue, "start")
