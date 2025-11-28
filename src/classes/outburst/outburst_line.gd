class_name OutburstLine
extends Outburst

@export var line:String = ""
@export_range(-1, 2, 0.1) var duration:float = -1

func say() -> void:
	await DialogueBalloon.say_outburst(line, duration)
