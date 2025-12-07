class_name DifficultySelect
extends Control

signal tab_changed(tab: int)

@export var stylebox_disabled: StyleBoxFlat
@export var stylebox_unselected: StyleBoxFlat
@export var stylebox_selected: StyleBoxFlat


var current_tab: int = -1:
	set = set_current_tab


func _ready() -> void:
	var children := get_children()
	for i in len(children):
		var tab: Button = children[i]
		tab.pressed.connect(set_current_tab.bind(i))


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action("ui_right"):
		if event.is_pressed():
			cycle_tab(1)
		get_viewport().set_input_as_handled()
		return
	if event.is_action("ui_left"):
		if event.is_pressed():
			cycle_tab(-1)
		get_viewport().set_input_as_handled()
		return


func set_tab_availability(tab:int, available:bool) -> void:
	(get_child(tab) as Button).disabled = !available


func set_current_tab(new: int) -> void:
	if current_tab == new:
		return
	if current_tab != -1:
		(get_child(current_tab) as Button).button_pressed = false
	assert(current_tab < get_child_count())
	assert(current_tab >= -1)
	current_tab = new
	if current_tab != -1:
		(get_child(current_tab) as Button).button_pressed = true
	tab_changed.emit(current_tab)


func cycle_tab(increment: int) -> void:
	if current_tab == -1:
		return
	assert(increment, "'increment' must be a nonzero value")
	var child_count := get_child_count()
	var tenative:int = wrapi(current_tab + increment, 0, child_count)
	while (get_child(tenative) as Button).disabled:
		tenative = wrapi(tenative + signi(increment), 0, child_count)
	current_tab = tenative
