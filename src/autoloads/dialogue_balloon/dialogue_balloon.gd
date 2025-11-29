extends CanvasLayer

const CHARACTER_INACTIVE_DOWNSHIFT := 15.0
const CHARACTER_INACTIVE_COLOR := Color(0.7, 0.7, 0.7)
const CHARACTER_INACTIVE_SCALE := Vector2(0.975, 0.975)

const CHARACTER_OUT_RIGHTSHIFT := -100


## The base balloon anchor
@onready var balloon: Control = %Balloon

## The label showing the name of the currently speaking character
@onready var character_label: Label = %CharacterLabel

@onready var character_label_panel: PanelContainer = %CharacterLabelPanel

# This sucks and I know it
@onready var character_anchor_dock: CanvasItem = %CharacterAnchor.get_child(0)
@onready var character_anchor_dock_bottom: CanvasItem = %CharacterAnchor.get_child(1)

## The label showing the currently spoken dialogue
@onready var dialogue_label: DialogueLabel = %DialogueLabel

@onready var outburst_anchor: Control = %OutburstAnchor
@onready var outburst_label: Label = %OutburstLabel

@onready var audio_stream_player:AudioStreamPlayer = $AudioStreamPlayer


var dialogue_resource: DialogueResource

var dialogue_line: DialogueLine:
	set(value):
		if value:
			dialogue_line = value
			apply_dialogue_line()
		else:
			close_balloon()

var gamestate_info:Array = [
	self
]

var character_active: bool
var character_anchor_tween: Tween
var outburst_tween: Tween


func _ready() -> void:
	balloon.hide()
	outburst_anchor.hide()
	character_anchor_dock.hide()
	character_anchor_dock.position.y = character_anchor_dock_bottom.position.y


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed(&"ui_accept"):
		get_viewport().set_input_as_handled()
		
		if dialogue_label.is_typing:
			dialogue_label.skip_typing()
		
		next(dialogue_line.next_id)


func start(p_dialogue_resource:DialogueResource, title:String) -> void:
	open_balloon()
	dialogue_resource = p_dialogue_resource
	next(title)


func play_audio(stream:AudioStream) -> void:
	audio_stream_player.stream = stream
	audio_stream_player.play()


func apply_dialogue_line() -> void:
	var character_name = dialogue_line.get_tag_value("name")
	if character_name.is_empty():
		character_name = dialogue_line.character
	
	var new_activity: bool = not character_name.is_empty()
	
	character_label_panel.visible = new_activity
	character_label.text = tr(character_name, "dialogue")
	
	if new_activity != character_active:
		character_anchor_tween = create_tween_overkill(character_anchor_tween, character_anchor_dock)
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


func move_auxie(move_in:bool, duration:float = 0.4) -> float:
	character_anchor_tween = create_tween_overkill(character_anchor_tween, character_anchor_dock)
	character_move(
		character_anchor_dock, move_in, character_anchor_tween, duration
	)
	return duration


func say_outburst(message:String, duration:float) -> void:
	outburst_label.text = message
	outburst_anchor.show()
	outburst_tween = create_tween_overkill(outburst_tween, outburst_anchor)
	outburst_tween.tween_property(outburst_label, ^"position:y", 0, 0.2)\
			.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)\
			.from(20)
	outburst_tween.parallel().tween_property(outburst_label, ^"modulate:a", 1, 0.2)\
			.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)\
			.from(0)
	outburst_tween.chain().tween_interval(duration)
	outburst_tween.chain().tween_property(outburst_label, ^"position:y", -20, 0.2)\
			.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_IN)
	outburst_tween.parallel().tween_property(outburst_label, ^"modulate:a", 0, 0.2)\
			.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_IN)
	outburst_tween.chain().tween_callback(outburst_anchor.hide)
	await outburst_tween.finished


func open_balloon() -> void:
	balloon.show()
	character_anchor_tween = create_tween_overkill(character_anchor_tween, character_anchor_dock)
	character_shift(
		character_anchor_dock, character_anchor_dock_bottom.position.y, true, character_anchor_tween
	)


func close_balloon() -> void:
	balloon.hide()
	character_anchor_tween = create_tween_overkill(character_anchor_tween, character_anchor_dock)
	character_shift(
		character_anchor_dock, character_anchor_dock_bottom.position.y, false, character_anchor_tween
	)


## Kills input tween if it is valid. Returns a newly created Tween from [param]tween_source[/param]
## if a source was provided, otherwise will return a tween created by the [SceneTree].
## Designed to be used as [code]tween_var = create_tween_overkill(tween_var, self)[/code]
## or similarly.
func create_tween_overkill(input:Tween, source:Node = null) -> Tween:
	if input and input.is_valid():
			input.kill()
	if source:
		return source.create_tween()
	return create_tween()


## Fade character to/from active state
func character_active_fade(
		node:CanvasItem, 
		activity:bool, 
		tween:Tween,
		duration:float = 0.25) -> void:
	
	if activity:
		tween.tween_property(node, ^"scale", Vector2.ONE, duration)\
				.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tween.parallel().tween_property(node, ^"position:y", 0, duration)\
				.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tween.parallel().tween_property(node, ^"self_modulate", Color.WHITE, duration)\
				.set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	else:
	
		tween.tween_property(node, ^"scale", CHARACTER_INACTIVE_SCALE, duration)\
				.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tween.parallel().tween_property(node, ^"position:y", CHARACTER_INACTIVE_DOWNSHIFT, duration)\
				.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tween.parallel().tween_property(node, ^"self_modulate", CHARACTER_INACTIVE_COLOR, duration)\
				.set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)


## Shift character up/down for dialogue box open/close
func character_shift(
		node:CanvasItem,
		bottom:float,
		shift_up:bool,
		tween:Tween, 
		duration:float = 0.25) -> void:
	if shift_up:
		tween.tween_property(node, ^"position:y", 0, duration)\
				.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT).from(bottom)
	else:
		tween.tween_property(node, ^"position:y", bottom, duration)\
				.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT).from(0)


## Move character in/out 
func character_move(
		node:CanvasItem,
		move_in:bool,
		tween:Tween, 
		duration:float = 0.4) -> void:
	if move_in:
		tween.tween_callback(node.show)
		tween.tween_property(node, ^"position:x", 0, duration)\
				.set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT).from(CHARACTER_OUT_RIGHTSHIFT)
		tween.parallel().tween_property(node, ^"self_modulate:a", 1, duration)\
				.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT).from(0)
	else:
		tween.tween_property(node, ^"position:y", CHARACTER_OUT_RIGHTSHIFT, duration)\
				.set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT).from(0)
		tween.parallel().tween_property(node, ^"self_modulate:a", 0, duration)\
				.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT).from(1)
		tween.tween_callback(node.hide)
