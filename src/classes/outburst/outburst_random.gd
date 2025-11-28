class_name OutburstRandom
extends Outburst

@export var lines:Array[Outburst]

func say() -> void:
	await lines.pick_random().say()
