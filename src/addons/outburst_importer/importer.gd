@tool
extends EditorImportPlugin


const BASE_AUDIO_DIALOGUE_PATH: String = "res://assets/audio/dialogue/"


var tag_map: Dictionary[String, Callable] = {
	"line":importer_mapped.bind(OutburstLine, {
		"@text":map_direct.bind(&"line"),
		"dur":map_basic.bind(&"duration"),
		"snd":map_resource.bind(&"stream", BASE_AUDIO_DIALOGUE_PATH, "AudioStream")
	}),
	"rand":importer_mapped.bind(OutburstRandom, {
		"@elements":map_direct.bind(&"lines")
	}),
	"seq":importer_mapped.bind(OutburstSequence, {
		"@elements":map_direct.bind(&"lines")
	}),
}


func _get_importer_name() -> String:
	return "outbursts"


func _can_import_threaded() -> bool:
	return true # I'm pretty sure this is fine


func _get_visible_name():
	return "Outburst Bank"


func _get_recognized_extensions():
	return ["outbank"]


func _get_save_extension():
	return "res"


func _get_resource_type():
	return "Resource"


func _get_import_options(path: String, preset_index: int) -> Array[Dictionary]:
	return []


func _import(
		source_file: String, 
		save_path: String, 
		options: Dictionary, 
		platform_variants: Array[String], 
		gen_files: Array[String]
	) -> Error:
	var parser := XMLParser.new()
	var err := parser.open(source_file)
	if err != OK:
		push_error("Unexpected error opening outbank file: ", error_string(err))
		return ERR_FILE_CANT_OPEN
	
	err = parser.read()
	if err != OK:
		push_error("Unexpected error reading outbank file: ", error_string(err))
		return ERR_PARSE_ERROR
	
	var type := parser.get_node_type()
	if type != XMLParser.NODE_ELEMENT:
		push_error("Unexpected node reading outbank file")
		return ERR_PARSE_ERROR
	var outburst := find_importer(parser)
	if outburst == null:
		return ERR_PARSE_ERROR
	
	return ResourceSaver.save(outburst, save_path + "." + _get_save_extension())


## Imports an XML tag as type. 'Map' defines the attribute -> variable map.
## the key "@text" will import the text of the tag, and "@elements" the elements.
func importer_mapped(
		parser: XMLParser,
		type: Script, 
		map: Dictionary,
		process_info: Dictionary = {},
	) -> Outburst:
	var instance: Outburst = type.new()
	
	for i in parser.get_attribute_count():
		var name := parser.get_attribute_name(i)
		if not name in map:
			push_error("attribute ", name, " does not exist on tag!")
			push_error("value: ", str_to_var(parser.get_attribute_value(i)))
			continue
		map[name].call(parser.get_attribute_value(i), instance)
	
	var text_combined: String = ""
	var elements_combined: Array[Outburst] = []
	while true:
		var err := parser.read()
		if err != OK:
			push_error("Error in XML reading: ", error_string(err))
			return null
		
		match parser.get_node_type():
			XMLParser.NodeType.NODE_TEXT:
				if "@text" in map:
					text_combined += parser.get_node_data()
				else:
					if not parser.get_node_data().strip_edges().is_empty():
						push_error("Text not supported for this tag!")
			XMLParser.NodeType.NODE_ELEMENT:
				if "@elements" in map:
					elements_combined.append(find_importer(parser))
				else:
					push_error("Tags not supported for this tag!")
			XMLParser.NODE_COMMENT:
				pass
			XMLParser.NodeType.NODE_ELEMENT_END:
				break
			_:
				push_error("Unexpected XML node: ", parser.get_node_type())
	if "@text" in map:
		map["@text"].call(text_combined, instance)
	if "@elements" in map:
		map["@elements"].call(elements_combined, instance)
	return instance


func find_importer(parser: XMLParser) -> Outburst:
	var tag_name := parser.get_node_name()
	if not tag_name in tag_map:
		push_error("No importer found for tag ", tag_name)
		return null
	return tag_map[tag_name].call(parser)

## Basic variable interpretation of value
func map_basic(value: String,target: Objecttproperty: StringNameme) -> void:
	target.set(property, str_to_var(value))

## Directly maps contents to value
func map_direct(value: Variant,target: Objecttproperty: StringNameme) -> void:
	target.set(property, value)

## Loads a resource from the value path (with optional base path and type hint)
func map_resource(
		value: String, 
		target: Object, 
		property: StringName,
		base_path: String = "",
		resource_type_hint: String = "",
	) -> void:
	var path := base_path.path_join(value)
	if not path.is_absolute_path():
		push_error('Path "%s" is not a valid resource path' % path)
		return
	if not ResourceLoader.exists(path, resource_type_hint):
		if resource_type_hint.is_empty():
			resource_type_hint = "Resource"
		push_error('No Resource of type %s exists at path "%s"' % [resource_type_hint, path])
		return
	target.set(property, ResourceLoader.load(path, "AudioStream"))
