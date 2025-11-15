extends CanvasLayer


## The base balloon anchor
@onready var balloon: Control = %Balloon

## The label showing the name of the currently speaking character
@onready var character_label: Label = %CharacterLabel

@onready var character_label_panel: PanelContainer = %CharacterLabelPanel

@onready var character_anchor_dock: Control = %CharacterAnchor.get_child(0)

## The label showing the currently spoken dialogue
@onready var dialogue_label: DialogueLabel = %DialogueLabel


var dialogue_resource: DialogueResource

var dialogue_line: DialogueLine:
	set(value):
		if value:
			dialogue_line = value
			apply_dialogue_line()
		else:
			hide()

var gamestate_info:Array = []

var character_active: bool
var character_anchor_tween: Tween


func _ready() -> void:
	hide()


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed(&"ui_accept"):
		get_viewport().set_input_as_handled()
		
		if dialogue_label.is_typing:
			dialogue_label.skip_typing()
		
		next(dialogue_line.next_id)


func start(p_dialogue_resource:DialogueResource, title:String) -> void:
	show()
	dialogue_resource = p_dialogue_resource
	next(title)


func apply_dialogue_line() -> void:
	var character_name = dialogue_line.get_tag_value("name")
	if character_name.is_empty():
		character_name = dialogue_line.character
	
	var new_activity: bool = not character_name.is_empty()
	
	character_label_panel.visible = new_activity
	character_label.text = tr(character_name, "dialogue")
	
	if new_activity != character_active:
		if character_anchor_tween and character_anchor_tween.is_valid():
			character_anchor_tween.kill()
		character_anchor_tween = create_tween().set_parallel()
		character_anchor_tween.set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
		
		character_active_fade(character_anchor_dock, new_activity, character_anchor_tween)
		character_active = new_activity
	
	dialogue_label.dialogue_line = dialogue_line
	dialogue_label.type_out()


func next(next_id: String) -> void:
	dialogue_line = await dialogue_resource.get_next_dialogue_line(next_id, gamestate_info)


func _on_balloon_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.is_pressed():
		get_viewport().set_input_as_handled()
		
		if dialogue_label.is_typing:
			dialogue_label.skip_typing()
			return
		
		next(dialogue_line.next_id)


func character_active_fade(
		node:CanvasItem, 
		activity:bool, 
		tween:Tween, 
		duration:float = 0.25) -> void:
	tween.set_parallel()
	if activity:
		tween.tween_property(node, ^"scale", Vector2.ONE, 
				duration).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tween.tween_property(node, ^"position:y", 0,
				duration).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tween.tween_property(node, ^"modulate", Color.WHITE, 
				duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	else:
		tween.tween_property(node, ^"scale", Vector2(0.975, 0.975), 
				duration).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tween.tween_property(node, ^"position:y", 15,
				duration).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tween.tween_property(node, ^"modulate", Color(0.7, 0.7, 0.7), 
				duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
