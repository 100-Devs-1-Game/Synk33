@tool
extends VBoxContainer


@export var info:String = "":
	set(new):
		info = new
		if not is_node_ready():
			return
		info_label.text = info

@export var type:String = "":
	set(new):
		type = new
		if not is_node_ready():
			return
		type_label.text = type

@onready var info_label:Label = $Info
@onready var type_label:Label = $Type


func _ready() -> void:
	info = info
	type = type
