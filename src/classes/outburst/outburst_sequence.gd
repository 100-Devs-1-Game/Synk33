class_name OutburstSequence
extends Outburst

@export var lines:Array[Outburst]

func say() -> void:
	for line in lines:
		await line.say()
