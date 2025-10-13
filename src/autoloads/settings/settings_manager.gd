extends Node

const OPTIONS_SAVE_PATH:String = "user://options.cfg"

var current:SettingsSave
## Options duplicate used as a scratchboard for all the settings that are not yet
## applied
var tenative:SettingsSave


func _ready() -> void:
	current = SettingsSave.load_config(OPTIONS_SAVE_PATH)
	current.apply_settings(get_tree().root)
	current.save_config(OPTIONS_SAVE_PATH)
	tenative = current.duplicate()


func apply_tenative() -> void:
	current = tenative
	current.apply_settings(get_tree().root)
	current.save_config(OPTIONS_SAVE_PATH)
	tenative = current.duplicate()


func cancel_tenative() -> void:
	tenative = current.duplicate()
