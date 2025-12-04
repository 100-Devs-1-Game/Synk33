class_name DifficultySelect
extends Control

signal tab_changed(tab:int)

@export var stylebox_disabled:StyleBoxFlat
@export var stylebox_unselected:StyleBoxFlat
@export var stylebox_selected:StyleBoxFlat


func _ready() -> void:
	var children := get_children()
	
	for i in len(children):
		var tab:Button = children[i]
		tab.pressed.connect(tab_changed.emit.bind(i))


func set_tab_availability(tab:int, available:bool) -> void:
	(get_child(tab) as Button).disabled = !available
