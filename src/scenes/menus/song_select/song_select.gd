extends Control


@export var songs: Array[Song] 


@onready var song_container: Container = %SongContainer
@onready var song_cover: TextureRect = %SongCover
@onready var song_info_length: Control = %LengthSongInfo

@onready var audio_stream_player: AudioStreamPlayer = $AudioStreamPlayer


static func time_as_string(time: float) -> String:
	return "%02d:%02d" % [time / 60, fmod(time, 60)]


func _ready() -> void:
	for song in songs:
		var song_button: SongButton = preload("res://scenes/menus/song_select/song_button.tscn").instantiate()
		song_button.icon = song.Cover
		song_button.title = song.Name
		song_button.credit = song.Author
		song_button.focus_entered.connect(change_song.bind(song))
		song_container.add_child(song_button)

func change_song(to: Song) -> void:
	song_cover.texture = to.Cover
	song_info_length.info = time_as_string(to.Audio.get_length())
	audio_stream_player.stream = to.Audio
	audio_stream_player.play()
