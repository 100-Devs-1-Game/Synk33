@tool
extends Container


const TARGET_SNAP_MARGIN:float = 0.01


@export var scale_curve:Curve:
	set(new):
		if scale_curve != null:
			scale_curve.changed.disconnect(queue_sort)
		scale_curve = new
		if scale_curve != null:
			scale_curve.changed.connect(queue_sort)


var selected:float = 0.0:
	set(new):
		if selected == new:
			return
		selected = new
		queue_sort()
var target_selected:int = 0
var _mousewrap_offset:float = 0
var _cumulative:float = 0


func _init() -> void:
	_update_repeat(0)


func _process(delta: float) -> void:
	if Engine.is_editor_hint():
		return
	
	if absf(target_selected - selected) < TARGET_SNAP_MARGIN:
		selected = target_selected
		return
	selected = lerpf(selected, target_selected, delta * 4)


func _notification(what: int) -> void:
	match what:
		NOTIFICATION_CHILD_ORDER_CHANGED:
			_child_order_changed()
		NOTIFICATION_SORT_CHILDREN:
			_sort_children()
		NOTIFICATION_TRANSLATION_CHANGED, NOTIFICATION_LAYOUT_DIRECTION_CHANGED:
			queue_sort()


func _input(event: InputEvent) -> void:
	if not visible:
		return
	if event is InputEventMouseButton: #We
		var global_rect := get_global_rect()
		if not global_rect.has_point(event.global_position):
			return
		if not event.pressed:
			return
		
		assert(
			event.global_position.is_equal_approx(event.position), 
			"SongSelect container cannot have a mismatch between viewport local and global input coordinates"
		) 
		
		event.global_position = _wrap_mouse(event.global_position)
		# TODO: Make this system compatible with viewport local coordinates.
		# I'm lazy and don't want to find the correct matrix
		event.position = event.global_position


func _get_allowed_size_flags_horizontal() -> PackedInt32Array:
	return [SIZE_FILL, SIZE_SHRINK_BEGIN, SIZE_SHRINK_CENTER, SIZE_SHRINK_END]

func _get_allowed_size_flags_vertical() -> PackedInt32Array:
	return [SIZE_SHRINK_BEGIN]


func _wrap_mouse(gpos:Vector2) -> Vector2:
	var global_rect := get_global_rect()
	var minv:float = _mousewrap_offset + global_rect.size.y / 2.0 + global_rect.position.y
	var maxv:float = _mousewrap_offset + _cumulative + global_rect.size.y / 2.0 + global_rect.position.y
	gpos.y = wrapf(
		gpos.y,
		minv,
		maxv
	)
	return gpos 


func _child_order_changed() -> void:
	var count:int = get_child_count()
	for i in count:
		var child:Control = get_child(i)
		if Engine.is_editor_hint():
			return
		if child.focus_entered.is_connected(_goto):
			child.focus_entered.disconnect(_goto)
		child.focus_entered.connect(_goto.bind(i))
		
		if child.gui_input.is_connected(_on_label_gui_input):
			child.gui_input.disconnect(_on_label_gui_input)
		child.gui_input.connect(_on_label_gui_input.bind(i))
		
		child.focus_neighbor_top = child.get_path_to(get_child(wrapi(i - 1, 0, count)))
		child.focus_neighbor_bottom = child.get_path_to(get_child(wrapi(i + 1, 0, count)))


func _sort_children() -> void:
	if not is_instance_valid(scale_curve):
		return
	var count:int = get_child_count()
	var children := get_children()
	
	_cumulative = 0.0
	_mousewrap_offset = 0
	
	var select_centered_cumulative:float = 0.0
	
	for i in count:
		if children[i] is not Control:
			continue
		
		var child:Control = children[i]
		child.scale = Vector2.ONE * scale_curve.sample(wrapf(i - selected, -count / 2.0, count / 2.0))
		child.position.y = _cumulative
		_cumulative += child.get_rect().size.y
		_size_child(child)
		
		select_centered_cumulative -= child.get_rect().size.y * clampf(
			wrapf(selected + 0.5, 0, count) - i, 0, 1)
	
	for i in count:
		if children[i] is not Control:
			continue
		
		var child:Control = children[i]
		child.position.y += select_centered_cumulative + size.y / 2.0
	_mousewrap_offset = select_centered_cumulative
	_update_repeat(_cumulative)


func _size_child(child:Control) -> void:
	var child_size := child.get_combined_minimum_size()
	child.position.x = 0
	
	if child.size_flags_horizontal & SIZE_FILL:
		child_size.x = size.x / child.scale.x
	if child.size_flags_horizontal & SIZE_SHRINK_CENTER:
		child.position.x = (size.x - child_size.x * child.scale.x) / 2.0
	if child.size_flags_horizontal & SIZE_SHRINK_END:
		child.position.x = size.x - child_size.x * child.scale.x
	
	child.size = child_size


func _goto(index:int) -> void:
	var period:int = get_child_count()
	var period_half:float = period / 2.0
	var delta:int = (index - target_selected) % period
	if delta < -period_half:
		selected -= period
	elif delta > period_half:
		selected += period
	target_selected = index


func _update_repeat(childsize:float) -> void:
	RenderingServer.canvas_set_item_repeat(get_canvas_item(), Vector2(0, childsize), 2)


func _on_label_gui_input(event: InputEvent, index:int) -> void:
	if event is InputEventMouseButton:
		if event.button_index != MOUSE_BUTTON_LEFT:
			return
		if not event.is_pressed():
			return
		(get_child(index) as Control).grab_focus()
