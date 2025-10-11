extends Control
@onready var game_manager = $"../GameManager"
@onready var conductor: Conductor = $"../Conductor"
@onready var input_manager = $"../InputManager"
@onready var judgement_manager = $"../JudgementManager"
@onready var time = $Time

func _process(delta: float): 
	time.text = "%s \n %s" % [(Time.get_unix_time_from_system() - conductor.StartingTimestamp), conductor.SongPosition]
