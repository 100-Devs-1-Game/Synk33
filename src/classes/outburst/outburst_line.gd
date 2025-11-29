class_name OutburstLine
extends Outburst


## Raw text to display
@export var line:String = ""
## How long the outburst should stay visible. This does not factor in the time it
## takes for the outburst bubble to appear/disappear, so in practice the minimum
## is 0.4 seconds at of time of writing. If set to -1, the length is automatically
## determined.
@export_range(-1, 1, 0.1, "or_greater") var duration:float = -1
## The audio to play with the outburst
@export var stream:AudioStream

func say() -> void:
	var resolved_duration:float = duration
	if stream:
		if resolved_duration == -1:
			resolved_duration = stream.get_length() - 0.4
		DialogueBalloon.play_audio(stream)
	await DialogueBalloon.say_outburst(line, resolved_duration)
