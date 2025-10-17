@tool
extends Container

@export var selected:float = 0.0:
	set(new):
		selected = new
		queue_sort()


@export var scale_curve:Curve:
	set(new):
		if scale_curve != null:
			scale_curve.changed.disconnect(queue_sort)
		scale_curve = new
		if scale_curve != null:
			scale_curve.changed.connect(queue_sort)

var target_selected:float = selected


func _ready() -> void:
	target_selected = selected


func _process(delta: float) -> void:
	if Engine.is_editor_hint():
		return
	selected = lerpf(selected, target_selected, delta * 4)


func _goto(index:int) -> void:
	target_selected = index


func _notification(what: int) -> void:
	match what:
		NOTIFICATION_CHILD_ORDER_CHANGED:
			var count:int = get_child_count()
			for i in count:
				var child:Control = get_child(i)
				if Engine.is_editor_hint():
					return
				if child.focus_entered.is_connected(_goto): 
					# This should work. I'm PRETTY sure bound
					# callables are considered the same. In the offchance they 
					# aren't you'll be getting errors here anyway
					child.focus_entered.disconnect(_goto)
				child.focus_entered.connect(_goto.bind(i))
				child.focus_neighbor_top = child.get_path_to(get_child(wrapi(i - 1, 0, count)))
				child.focus_neighbor_bottom = child.get_path_to(get_child(wrapi(i + 1, 0, count)))
		NOTIFICATION_SORT_CHILDREN:
			_sort_children()


func _sort_children() -> void:
	if not is_instance_valid(scale_curve):
		return
	var count:int = get_child_count()
	
	var cumulative:float = 0.0
	for i in count:
		var child:Control = get_child(i)
		
		child.size = child.get_minimum_size()
		child.scale = Vector2.ONE * scale_curve.sample(wrapf(i - selected, -count / 2.0, count / 2.0))
		child.position.y = cumulative
		cumulative += child.get_rect().size.y
	
