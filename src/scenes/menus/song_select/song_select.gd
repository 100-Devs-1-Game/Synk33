extends Control


enum Difficulty {
	Easy,
	Normal,
	Hard,
	Expert
}



@export var songs:Array[Song] 


var current_difficulty:Difficulty:
	set(new):
		if current_difficulty == new:
			return
		current_difficulty = new
		filter_songs()


@onready var song_container:Container = %SongContainer
@onready var song_cover:TextureRect = %SongCover
@onready var song_info_bpm:Control = %BPMSongInfo
@onready var song_info_length:Control = %LengthSongInfo
@onready var song_info_highscore:Control = %HighscoreSongInfo

@onready var difficulty_select:TabBar = %DifficultySelect
@onready var audio_stream_player:AudioStreamPlayer = $AudioStreamPlayer


static func time_as_string(time:float) -> String:
	return "%02d:%02d" % [time / 60, fmod(time, 60)]


func _ready() -> void:
	for song in songs:
		var song_button:SongButton = preload("res://scenes/menus/song_select/song_button.tscn").instantiate()
		song_button.icon = song.Cover
		song_button.title = song.Name
		song_button.credit = song.Author
		song_button.focus_entered.connect(change_song.bind(song))
		song_container.add_child(song_button)
	filter_songs()


func change_song(to:Song) -> void:
	var chart:Chart = to.GetChartByDifficulty(current_difficulty)
	if chart == null:
		assert(false, "No chart found for current difficulty")
		return
	change_chart(chart)


func change_chart(to:Chart) -> void:
	song_cover.texture = to.Song.Cover
	song_info_bpm.info = "%03dBPM" % to.Song.Bpm
	song_info_length.info = time_as_string(to.Song.Audio.get_length())
	
	song_info_highscore.info = str(SaveManager.GetChartPerformance(
		ResourceLoader.get_resource_uid(to.resource_path)
	))
	audio_stream_player.stream = to.Song.Audio
	audio_stream_player.play()


func filter_songs() -> void:
	# This is weird and I don't like that we aren't looping
	# over the actual song buttons
	for i in len(songs): 
		var song:Song = songs[i]
		if not song.HasChart(current_difficulty):
			(song_container.get_child(i) as Control).hide()
			continue
		(song_container.get_child(i) as Control).show()


func _on_difficulty_select_tab_changed(tab: int) -> void:
	current_difficulty = tab as Difficulty
