class_name JoypadConverter
extends Object


const _JOYBUTTON_NAMES: PackedStringArray = [
	"A",
	"B",
	"X",
	"Y",
	"Back",
	"Guide",
	"Start",
	"LeftStick",
	"RightStick",
	"LeftShoulder",
	"RightShoulder",
	"DPadUp",
	"DPadDown",
	"DPadLeft",
	"DPadRight",
	"Misc",
	"Paddle1",
	"Paddle2",
	"Paddle3",
	"Paddle4",
	"Touchpad",
]
const _UNKNOWN_BUTTON_PREFIX: String = "Button"

const _JOYAXIS_NAMES: PackedStringArray = [
	"LeftX",
	"LeftY",
	"RightX",
	"RightY",
	"LeftTrigger",
	"RightTrigger"
]
const _UNKNOWN_AXIS_PREFIX: String = "Axis"

# A ""constant"" reverse lookup table for _JOYBUTTON_NAMES
static var _JOYBUTTON_NAME_REVERSE_LOOKUP: Dictionary[String, JoyButton] = {}
# Ditto prev for _JOYAXIS_NAMES
static var _JOYAXIS_NAME_REVERSE_LOOKUP: Dictionary[String, JoyAxis] = {}

static var _joyaxis_regex: RegEx = RegEx.create_from_string(r"(-)?(.+)")


static func _static_init() -> void:
	for i in len(_JOYBUTTON_NAMES):
		_JOYBUTTON_NAME_REVERSE_LOOKUP[_JOYBUTTON_NAMES[i]] = i as JoyButton
	_JOYBUTTON_NAME_REVERSE_LOOKUP.make_read_only() 
	for i in len(_JOYAXIS_NAMES):
		_JOYAXIS_NAME_REVERSE_LOOKUP[_JOYAXIS_NAMES[i]] = i as JoyAxis
	_JOYAXIS_NAME_REVERSE_LOOKUP.make_read_only()


## Returns [code]true[/code] if a proper name for the [enum JoyButton] is defined (in practice,
## every button defined by the SDL should have a name defined).
static func joybutton_has_name(button: JoyButton) -> bool:
	return button >= 0 and button < len(_JOYBUTTON_NAMES)


## Returns [code]true[/code] if a proper name for the [enum JoyAxis] is defined (in practice,
## every axis defined by the SDL should have a name defined).
static func joyaxis_has_name(axis: JoyAxis) -> bool:
	return axis >= 0 and axis < len(_JOYAXIS_NAMES)


## Returns the proper name for the [enum JoyButton] if one is defined (see [method joybutton_has_name]
## ), or a generic one as a fallback.
## [codeblock]
## print(JoypadConverter.get_joybutton_string(JOY_BUTTON_A))                    # Prints "A"
## print(JoypadConverter.get_joybutton_string(JOY_BUTTON_BACK))                 # Prints "Back"
## print(JoypadConverter.get_joybutton_string(100 as JoyButton))                # Prints "Button100"
## [/codeblock]
static func get_joybutton_string(button: JoyButton) -> String:
	if not joybutton_has_name(button):
		# Fallback to an int version to preserve compat with buttons outside the SDL
		return _UNKNOWN_BUTTON_PREFIX + str(button)
	return _JOYBUTTON_NAMES[button]


## Returns the proper name for the [enum JoyAxis] if one is defined (see [method joyaxis_has_name]
## ), or a generic one as a fallback. A negative symbol is appended if [param negative] is 
## [code]true[/code].
## [codeblock]
## print(JoypadConverter.get_joyaxis_string(JOY_AXIS_LEFT_X))                   # Prints "LeftX"
## print(JoypadConverter.get_joyaxis_string(JOY_AXIS_TRIGGER_LEFT, true))       # Prints "-LeftTrigger"
## print(JoypadConverter.get_joyaxis_string(8 as JoyAxis))                      # Prints "Axis8"
## [/codeblock]
static func get_joyaxis_string(axis: JoyAxis,negative: booll = false) -> String:
	var constructed: String = ""
	if not joyaxis_has_name(axis):
		constructed = _UNKNOWN_AXIS_PREFIX + str(axis)
	else:
		constructed = _JOYAXIS_NAMES[axis]
	if negative:
		constructed = "-" + constructed
	return constructed


## Returns the proper name for the Joypad [InputEvent] (see [method get_joybutton_string] 
## and [method get_joyaxis_string]). Asserts false if [param event] is not an 
## [InputEventJoypadButton] or [InputEventMotion].
static func get_joypad_event_string(event: InputEvent) -> String:
	if event is InputEventJoypadButton:
		return get_joybutton_string(event.button_index)
	if event is InputEventJoypadMotion:
		return get_joyaxis_string(event.axis, event.axis_value < 0)
	
	@warning_ignore("assert_always_false")
	assert(false, "event is not of an accepted type!")
	return ""


## Finds the [enum JoyButton] corresponding to the [String], or 
## [constant @GlobalScope.JOY_BUTTON_INVALID] if one couldn't be found.
static func find_joybutton_from_string(string: String) -> JoyButton:
	if _JOYBUTTON_NAME_REVERSE_LOOKUP.has(string):
		return _JOYBUTTON_NAME_REVERSE_LOOKUP[string]
	if string.begins_with(_UNKNOWN_BUTTON_PREFIX):
		var sub := string.substr(len(_UNKNOWN_BUTTON_PREFIX))
		if not sub.is_valid_int():
			return JOY_BUTTON_INVALID
		return int(sub) as JoyButton
	return JOY_BUTTON_INVALID


## Finds the [enum JoyAxis] corresponding to the [String], or 
## [constant @GlobalScope.JOY_AXIS_INVALID] if one couldn't be found.
static func find_joyaxis_from_string(string: String) -> JoyAxis:
	if _JOYAXIS_NAME_REVERSE_LOOKUP.has(string):
		return _JOYAXIS_NAME_REVERSE_LOOKUP[string]
	if string.begins_with(_UNKNOWN_AXIS_PREFIX) and string.substr(len(_UNKNOWN_AXIS_PREFIX)).is_valid_int():
		var sub := string.substr(len(_UNKNOWN_AXIS_PREFIX))
		if not sub.is_valid_int():
			return JOY_AXIS_INVALID
		return int(sub) as JoyAxis
	return JOY_AXIS_INVALID


static func joypad_input_event_from_string(string: String) -> InputEvent:
	var button_index := find_joybutton_from_string(string)
	if button_index != JOY_BUTTON_INVALID:
		var event := InputEventJoypadButton.new()
		event.button_index = button_index
		event.pressed = true
		return event
	
	var axis_match := _joyaxis_regex.search(string)
	if axis_match != null:
		var axis_name := axis_match.get_string(2)
		var axis_index := find_joyaxis_from_string(axis_name)
		if axis_index != JOY_AXIS_INVALID:
			var event := InputEventJoypadMotion.new()
			event.axis = axis_index
			event.axis_value = 1 if axis_match.get_string(1).is_empty() else -1
			return event
	
	return null
