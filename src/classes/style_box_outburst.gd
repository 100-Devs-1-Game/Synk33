@tool
class_name StyleBoxOutburst
extends StyleBox

@export var body_color: Color = Color.BLACK:
	set(new):
		if body_color != new:
			body_color = new
			changed.emit()
@export var outline_color: Color = Color.WHITE:
	set(new):
		if outline_color != new:
			outline_color = new
			changed.emit()

@export var outline_thickness: float = 6.0:
	set(new):
		if outline_thickness != new:
			outline_thickness = new
			changed.emit()
@export var outerline_thickness: float = 3.0:
	set(new):
		if outerline_thickness != new:
			outerline_thickness = new
			changed.emit()

@export var top_left_offset := Vector2(0,0):
	set(new):
		if top_left_offset != new:
			top_left_offset = new
			changed.emit()
@export var top_right_offset := Vector2(0,0):
	set(new):
		if top_right_offset != new:
			top_right_offset = new
			changed.emit()
@export var bottom_left_offset := Vector2(0,0):
	set(new):
		if bottom_left_offset != new:
			bottom_left_offset = new
			changed.emit()
@export var bottom_right_offset := Vector2(0,0):
	set(new):
		if bottom_right_offset != new:
			bottom_right_offset = new
			changed.emit()
@export var tail_offset := Vector2(0,20):
	set(new):
		if tail_offset != new:
			tail_offset = new
			changed.emit()


func _draw(canvas_item: RID, rect: Rect2) -> void:
	var bottom_right := rect.position + Vector2(rect.size.x, rect.size.y) + bottom_right_offset
	var bottom_left := rect.position + Vector2(0, rect.size.y) + bottom_left_offset
	
	var body_points := PackedVector2Array([
		rect.position + top_left_offset,
		rect.position + Vector2(rect.size.x, 0) + top_right_offset,
		bottom_right,
		bottom_left.move_toward(bottom_right, 40),
		bottom_left.move_toward(bottom_right, 40) + tail_offset,
		bottom_left.move_toward(bottom_right, 20),
		bottom_left,
	])
	var colors := PackedColorArray()
	colors.resize(len(body_points))
	colors.fill(body_color)
	RenderingServer.canvas_item_add_polygon(
		canvas_item,
		body_points,
		colors
	)
	
	var outerline: Array[PackedVector2Array] = Geometry2D.offset_polygon(
		body_points, outline_thickness / 2.0 + outerline_thickness, Geometry2D.JOIN_MITER
	)
	for polygon in outerline:
		colors.resize(len(polygon))
		colors.fill(body_color)
		polygon.append(polygon[0])
		RenderingServer.canvas_item_add_polyline(
			canvas_item,
			polygon,
			colors,
			outerline_thickness,
			true
		)
	body_points.append(body_points[0])
	colors.resize(len(body_points))
	colors.fill(outline_color)
	RenderingServer.canvas_item_add_polyline(
		canvas_item,
		body_points, 
		colors,
		outline_thickness,
		true
	)
