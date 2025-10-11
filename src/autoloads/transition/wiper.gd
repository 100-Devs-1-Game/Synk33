@tool
extends ColorRect


func _init() -> void:
	var canvas_item := get_canvas_item()
	RenderingServer.canvas_item_set_canvas_group_mode(
		canvas_item, RenderingServer.CANVAS_GROUP_MODE_TRANSPARENT
	)
