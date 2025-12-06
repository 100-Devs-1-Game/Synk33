@tool
class_name ScaleContainer
extends Container

## Scale of the contents inside. The minimum size is determined by the regular minimum
## times this value.
@export_custom(
	PROPERTY_HINT_LINK, ""
) var contents_scale: Vector2 = Vector2.ONE:
	set(new):
		if contents_scale == new:
			return
		contents_scale = new
		queue_sort()
		update_minimum_size()


func _notification(what: int) -> void:
	match what:
		NOTIFICATION_SORT_CHILDREN:
			_sort_children()


func _sort_children() -> void:
	var unscaled_size := size / contents_scale
	if not unscaled_size.is_finite():
		unscaled_size = Vector2.ZERO
	
	for child in get_children():
		if child is not Control:
			continue
		child.position = Vector2.ZERO
		child.size = unscaled_size
		child.scale = contents_scale


func _get_minimum_size() -> Vector2:
	# Calling get_minimum_size here causes an
	# infinite loop as you might expect
	var largest: Vector2 = custom_minimum_size 
	
	for child in get_children():
		if child is not Control:
			continue
		var child_minsize: Vector2 = (child as Control).get_combined_minimum_size()
		largest = largest.max(child_minsize)
	
	largest *= contents_scale
	
	return largest
