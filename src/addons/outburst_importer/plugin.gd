@tool
extends EditorPlugin

var importer = load("res://addons/outburst_importer/importer.gd").new()


func _enable_plugin() -> void:
	pass


func _disable_plugin() -> void:
	pass


func _enter_tree() -> void:
	add_import_plugin(importer)

func _exit_tree() -> void:
	# Clean-up of the plugin goes here.
	pass
