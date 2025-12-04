extends Button

@export var color:Color
@export var selected:StyleBox
@export var unselected:StyleBox

func _ready() -> void:
	selected.bg_color = color
	unselected.border_color = color
