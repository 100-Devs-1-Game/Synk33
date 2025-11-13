extends CanvasLayer

## The base balloon anchor
@onready var balloon: Control = %Balloon

## The label showing the name of the currently speaking character
@onready var character_label: Label = %CharacterLabel

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


func _ready() -> void:
	hide()


func start(p_dialogue_resource:DialogueResource, title:String) -> void:
	show()
	dialogue_resource = p_dialogue_resource
	next(title)


func apply_dialogue_line() -> void:
	var character_name = dialogue_line.get_tag_value("name")
	if character_name.is_empty():
		character_name = dialogue_line.character
	
	character_label.visible = not character_name.is_empty()
	character_label.text = tr(character_name, "dialogue")
	
	dialogue_label.dialogue_line = dialogue_line
	dialogue_label.type_out()


func next(next_id: String) -> void:
	dialogue_line = await dialogue_resource.get_next_dialogue_line(next_id, gamestate_info)


func _on_balloon_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.is_pressed()\
			or event.is_action_pressed("ui_accept"):
		get_viewport().set_input_as_handled()
		
		if dialogue_label.is_typing:
			dialogue_label.skip_typing()
			return
		
		next(dialogue_line.next_id)
