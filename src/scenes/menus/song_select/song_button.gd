@tool
class_name SongButton
extends BaseButton

const _THEME_TYPE:StringName = &"SongButton"
enum {
	_SHAPED_TITLE,
	_SHAPED_CREDIT
}


@export var title:String:
	set(new):
		if title == new:
			return
		title = new
		_set_shaped_text(_shaped_text[_SHAPED_TITLE], new)
		queue_redraw()
@export var credit:String:
	set(new):
		if credit == new:
			return
		credit = new
		_set_shaped_text(_shaped_text[_SHAPED_CREDIT], new)
		queue_redraw()

@export var icon:Texture2D:
	set(new):
		if icon == new:
			return
		if icon != null:
			icon.changed.disconnect(queue_redraw)
		icon = new
		if icon != null:
			icon.changed.connect(queue_redraw)
		queue_redraw()


var text_server:TextServer = TextServerManager.get_primary_interface()
var _shaped_text:Array[RID] = []

#region themecache
var _tc_font_colors:PackedColorArray

var _tc_icon_seperation:int

var _tc_font:Font

var _tc_font_size:int

var _tc_focus:StyleBox
#endregion themecache


func _notification(what: int) -> void:
	match what:
		NOTIFICATION_THEME_CHANGED:
			_update_themecache()


func _init() -> void:
	_shaped_text = [text_server.create_shaped_text(
		TextServer.DIRECTION_AUTO, TextServer.ORIENTATION_HORIZONTAL
	), text_server.create_shaped_text(
		TextServer.DIRECTION_AUTO, 
		TextServer.ORIENTATION_HORIZONTAL
	)]


func _update_themecache() -> void:
	_tc_font_colors = [
		get_theme_color(&"font_title_color", _THEME_TYPE),
		get_theme_color(&"font_title_focus_color", _THEME_TYPE),
		get_theme_color(&"font_credit_color", _THEME_TYPE),
		get_theme_color(&"font_credit_focus_color", _THEME_TYPE),
	]
	
	_tc_icon_seperation = get_theme_constant(&"seperation", _THEME_TYPE)
	
	_tc_font = get_theme_font(&"font", _THEME_TYPE)
	
	_tc_font_size = get_theme_font_size(&"font_size", _THEME_TYPE)
	
	_tc_focus = get_theme_stylebox(&"focus", _THEME_TYPE)
	
	_set_shaped_text(_shaped_text[_SHAPED_TITLE], title)
	_set_shaped_text(_shaped_text[_SHAPED_CREDIT], credit)
	update_minimum_size()
	queue_redraw()


func _draw() -> void:
	RenderingServer.canvas_item_clear(get_canvas_item())
	var offset:Vector2
	
	if is_instance_valid(icon):
		var icon_size := _get_icon_size(size.y)
		draw_texture_rect(
			icon, 
			Rect2(
				Vector2.ZERO, icon_size
			), false
		)
		offset.x += icon_size.x + _tc_icon_seperation
	
	offset.y += (size.y - get_combined_minimum_size().y) / 2
	
	for i in len(_shaped_text):
		var shaped := _shaped_text[i]
		offset.y += text_server.shaped_text_get_ascent(shaped)
		text_server.shaped_text_draw(
			shaped, get_canvas_item(), offset, -1, -1, 
			_get_font_color(i)
		)
		offset.y += text_server.shaped_text_get_descent(shaped)
	
	if has_focus():
		if is_instance_valid(_tc_focus):
			_tc_focus.draw(get_canvas_item(), Rect2(
				Vector2.ZERO, get_combined_minimum_size()
			))
	


func _get_minimum_size() -> Vector2:
	var title_minsize:Vector2 = _get_shaped_size(_SHAPED_TITLE)
	var credit_minsize:Vector2 = _get_shaped_size(_SHAPED_CREDIT)
	var constructed := Vector2(
		maxf(title_minsize.x, credit_minsize.x),
		title_minsize.y + credit_minsize.y
	)
	constructed.x += _get_icon_size(constructed.y).x + _tc_icon_seperation
	return constructed


func _get_shaped_size(shaped_index:int) -> Vector2:
	if not is_instance_valid(_tc_font):
		return Vector2.ZERO
	var line_size:Vector2 = text_server.shaped_text_get_size(_shaped_text[shaped_index])
	
	var font_h:float = _tc_font.get_height(_tc_font_size)
	var asc:float = text_server.shaped_text_get_ascent(_shaped_text[shaped_index])
	var dsc:float = text_server.shaped_text_get_descent(_shaped_text[shaped_index])
	
	if asc + dsc < font_h:
		var diff:float = font_h - (asc + dsc)
		asc += diff / 2
		dsc += diff - (diff / 2)
	line_size.y = asc + dsc
	
	return line_size

func _get_font_color(shaped_index:int) -> Color:
	var index = shaped_index * 2
	if has_focus():
		index += 1
	return _tc_font_colors[index]


func _get_icon_size(height:float) -> Vector2:
	if not is_instance_valid(icon):
		return Vector2.ZERO
	
	var tex_size:Vector2 = Vector2(
		icon.get_width() * height / icon.get_width(),
		height
	)
	return tex_size


func _set_shaped_text(shaped:RID, string:String) -> void:
	if not is_instance_valid(_tc_font):
		return
	text_server.shaped_text_clear(shaped)
	text_server.shaped_text_add_string(shaped, string, _tc_font.get_rids(), _tc_font_size)
