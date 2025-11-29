@tool
extends EditorPlugin


var importer:EditorImportPlugin


func _enable_plugin() -> void:
	pass


func _disable_plugin() -> void:
	remove_import_plugin(importer)


func _enter_tree() -> void:
	importer = load("res://addons/outburst_importer/importer.gd").new()
	add_import_plugin(importer)

func _exit_tree() -> void:
	# Clean-up of the plugin goes here.
	pass
