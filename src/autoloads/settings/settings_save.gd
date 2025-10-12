class_name SettingsSave
extends Resource
## Saves options for easy referencing


const CATEGORY_GAMEPLAY := "gameplay"
const CATEGORY_VIDEO := "video"
const CATEGORY_AUDIO := "audio"


const MAXIMUM_LOOK_SENSITIVITY:float = 1 / PI / 30
const WINDOW_MODE_NAMES:PackedStringArray = [
	"windowed", "minimized", "maximized", "fullscreen", "exclusive_fullscreen"
]
const VSYNC_MODE_NAMES:PackedStringArray = [
	"disabled", "enabled", "adaptive", "mailbox"
]


#region Gameplay
@export_group("Gameplay")
@export_range(0, 1.0, 0.001, "or_greater") var custom_offset:float = 0.0
#endregion Gameplay

#region Video
@export_group("Video")
## Mode of the main window.
@export var window_mode:DisplayServer.WindowMode = DisplayServer.WINDOW_MODE_WINDOWED
@export var vsync_mode:DisplayServer.VSyncMode = DisplayServer.VSYNC_ENABLED
@export var max_fps:int = 0
@export var borderless:bool = false
#endregion Video

#region Audio
@export_group("Audio")
## Linear volumes of busses.
@export var bus_volumes:Array[float] = [
	1.0
]
#endregion Audio


static func load_config(path:String) -> SettingsSave:
	var instance := SettingsSave.new()
	if not FileAccess.file_exists(path):
		return instance
	
	var file := ConfigFile.new()
	var error := file.load(path)
	
	if error == ERR_FILE_CANT_OPEN:
		push_error("Can't open options config file at \"%s\", returning default options" % path)
		return instance
	
	instance._load_gameplay_settings(file)
	instance._load_video_settings(file)
	instance._load_audio_volumes(file)
	return instance


static func stringnum_deserialize(
			file:ConfigFile, 
			section:String, 
			property:String, 
			stringnum:PackedStringArray, 
			default:int
		) -> int:
	
	if not file.has_section_key(section, property):
		return default
	
	var index = stringnum.find(file.get_value(
		section, property, ""
	))
	if index == -1:
		return default
	return index


func save_config(path:String) -> void:
	var file:ConfigFile = ConfigFile.new()
	
	file.set_value(CATEGORY_GAMEPLAY, "custom_offset", custom_offset)
	
	file.set_value(CATEGORY_VIDEO, "window_mode", WINDOW_MODE_NAMES[window_mode])
	file.set_value(CATEGORY_VIDEO, "vsync_mode", VSYNC_MODE_NAMES[vsync_mode])
	file.set_value(CATEGORY_VIDEO, "max_fps", max_fps)
	file.set_value(CATEGORY_VIDEO, "borderless", borderless)
	
	for bus_idx in len(bus_volumes):
		var bus_name := AudioServer.get_bus_name(bus_idx)
		file.set_value(CATEGORY_AUDIO, bus_name, bus_volumes[bus_idx])
	
	file.save(path)


func set_bus_volume(bus_index:int, linear_volume:float) -> void:
	assert(len(bus_volumes) > bus_index, "bus index '0' does not already exist in bus volumes!")
	bus_volumes[bus_index] = linear_volume


func apply_settings(root:Viewport) -> void:
	_apply_video_settings(root)
	_apply_bus_volumes(root)

#region Loaders
func _load_gameplay_settings(file:ConfigFile) -> void:
	custom_offset = file.get_value(CATEGORY_GAMEPLAY, "custom_offset", custom_offset)


func _load_video_settings(file:ConfigFile) -> void:
	window_mode = stringnum_deserialize(
		file, CATEGORY_VIDEO, "window_mode", WINDOW_MODE_NAMES, DisplayServer.WINDOW_MODE_WINDOWED
	) as DisplayServer.WindowMode
	vsync_mode = stringnum_deserialize(
		file, CATEGORY_VIDEO, "vsync_mode", VSYNC_MODE_NAMES, DisplayServer.VSYNC_ENABLED
	) as DisplayServer.VSyncMode
	
	max_fps = file.get_value(
		CATEGORY_VIDEO, "max_fps", max_fps
	)
	borderless = file.get_value(
		CATEGORY_VIDEO, "borderless", borderless
	)


func _load_audio_volumes(file:ConfigFile) -> void:
	if not file.has_section(CATEGORY_AUDIO):
		push_error("No Audio Volumes section present in config!")
		return
	
	var bus_names := file.get_section_keys(CATEGORY_AUDIO)
	for bus_name in bus_names:
		var bus_index := AudioServer.get_bus_index(bus_name)
		if bus_index == -1:
			push_error("Invalid bus name \"%s\" in options config" % bus_name)
			continue
		
		bus_volumes[bus_index] = file.get_value(
			"Audio Volumes", 
			bus_name, 
			bus_volumes[bus_index]
		)
#endregion Loaders

#region Appliers
func _apply_video_settings(_root:Viewport) -> void:
	DisplayServer.window_set_mode(window_mode)
	Engine.max_fps = max_fps
	DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, borderless)


func _apply_bus_volumes(_root:Viewport) -> void:
	for bus_index in len(bus_volumes):
		AudioServer.set_bus_volume_linear(bus_index, bus_volumes[bus_index])
#endregion Appliers
