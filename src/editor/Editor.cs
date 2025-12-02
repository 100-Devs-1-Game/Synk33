using System;
using System.Collections.Generic;
using Godot;
using SYNK33.chart;

namespace SYNK33.editor;

public partial class Editor : Control {
    [Export] public required Chart Chart;
    
    public EditorState State { get; private set; } = new();
    
    private AudioStreamPlayer? _audioStreamPlayer;
    private AudioStreamPlayer? _hitSoundPlayer;
    private AudioStreamPlayer? _holdSoundPlayer;
    
    private Label _selectedTimeLabel = null!;
    private Label _selectedLaneLabel = null!;
    private Label _statusLabel = null!;
    private Label _designerLabel = null!;
    private Label _difficultyLabel = null!;
    private Label _levelLabel = null!;
    private Label _bpmLabel = null!;
    private Label _beatsPerMeasureLabel = null!;
    private Label _noteModeLabel = null!;
    private Label _notesCountLabel = null!;
    
    private readonly HashSet<(int, int, double, NoteType)> _playedNotes = new();
    private readonly HashSet<(int, int, double, NoteType)> _activeHoldNotes = new();

    public override void _Ready() {
        base._Ready();
        InitializeAudioPlayers();
        InitializeUiElements();
        InitializeAudioSounds();
        UpdateInfoDisplay();
        GrabFocus();
    }

    private void InitializeAudioPlayers() {
        _audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        _hitSoundPlayer = new AudioStreamPlayer { MaxPolyphony = 8 };
        AddChild(_hitSoundPlayer);
        _holdSoundPlayer = new AudioStreamPlayer { MaxPolyphony = 3 };
        AddChild(_holdSoundPlayer);
    }

    private void InitializeUiElements() {
        _selectedTimeLabel = GetNode<Label>("%SelectedBeat");
        _selectedLaneLabel = GetNode<Label>("%SelectedLane");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _designerLabel = GetNode<Label>("%Designer");
        _difficultyLabel = GetNode<Label>("%Difficulty");
        _levelLabel = GetNode<Label>("%Level");
        _bpmLabel = GetNode<Label>("%BPM");
        _beatsPerMeasureLabel = GetNode<Label>("%BeatsPerMeasure");
        _noteModeLabel = GetNode<Label>("%NoteMode");
        _notesCountLabel = GetNode<Label>("%NotesCount");
    }

    private void InitializeAudioSounds() {
        EditorAudio.CreateHitSound(_hitSoundPlayer!);
        EditorAudio.CreateHoldSound(_holdSoundPlayer!);
    }

    public void UpdateInfoDisplay() {
        _designerLabel.Text = $"Designer: {Chart.Designer}";
        _difficultyLabel.Text = $"Difficulty: {Chart.Difficulty}";
        _levelLabel.Text = $"Level: {Chart.Level}";
        _bpmLabel.Text = $"BPM: {Chart.Bpm}";
        _beatsPerMeasureLabel.Text = $"Beats/Measure: {Chart.BeatsPerMeasure}";
        _notesCountLabel.Text = $"Notes: {Chart.Notes.Count}";
        UpdateNoteModeLabel();
    }
    
    private void UpdateNoteModeLabel() {
        var modeText = State.NoteMode switch {
            NoteMode.Tap => "Mode: Tap (T)",
            NoteMode.Hold => State.HoldNoteStart == null 
                ? "Mode: Hold (T) - Click start" 
                : "Mode: Hold (T) - Click end",
            _ => "Mode: Unknown"
        };
        if (State.IsTriplet) modeText += "  [Triplet]";
        _noteModeLabel.Text = modeText;
    }

    public override void _Process(double delta) {
        base._Process(delta);
        QueueRedraw();
        EditorSelection.SelectBeat(State, Chart, GetViewport().GetMousePosition(), Size.Y, _selectedTimeLabel);
        EditorSelection.SelectLane(State, GetViewport().GetMousePosition(), Size.X, _selectedLaneLabel);
        
        if (State.IsPlaying) {
            EditorPlayback.CheckAndPlayNoteHits(Chart, _audioStreamPlayer, _hitSoundPlayer, _holdSoundPlayer, _playedNotes, _activeHoldNotes);
        }
    }

    public override void _Draw() {
        base._Draw();
        var totalLaneWidth = EditorConstants.MaxLanes * EditorConstants.LaneWidth;
        var startX = (Size.X - totalLaneWidth) / 2;
        
        EditorDrawing.DrawLaneBoundaries(this, startX);
        EditorDrawing.DrawGridLines(this, _audioStreamPlayer, Chart, State.GetEffectiveSnapping(), State.Zoom, State.PanY);
        EditorDrawing.DrawNotes(this, Chart, State, _audioStreamPlayer);
        EditorDrawing.DrawPlayhead(this, _audioStreamPlayer, State.Zoom, State.PanY, Chart.Bpm);
        EditorDrawing.DrawSelector(this, Chart, State);
    }

    public override void _GuiInput(InputEvent @event) {
        base._GuiInput(@event);

        switch (@event) {
            case InputEventMouseButton mouseEvent:
                EditorInput.HandleMouseButton(mouseEvent, this);
                break;
            case InputEventMouseMotion mouseMotionEvent when State.IsDragging:
                EditorView.PanView(State, mouseMotionEvent, QueueRedraw);
                break;
            case InputEventKey keyEvent:
                EditorInput.HandleKeyInput(keyEvent, this);
                break;
        }
    }

    public void AddNote() => EditorNotePlacement.AddNote(Chart, State, _playedNotes, UpdateInfoDisplay, QueueRedraw);
    public void RemoveNote() => EditorNotePlacement.RemoveNote(Chart, State, UpdateInfoDisplay, QueueRedraw);

    public void TogglePlayback() {
        State.IsPlaying = !State.IsPlaying;
        if (State.IsPlaying) {
            EditorPlayback.PlayPreview(_audioStreamPlayer, State.SelectedTime, Chart, _playedNotes);
        } else {
            EditorPlayback.StopPreview(_audioStreamPlayer, _holdSoundPlayer, _playedNotes, _activeHoldNotes);
        }
    }

    public void ToggleNoteMode() {
        State.NoteMode = State.NoteMode == NoteMode.Tap ? NoteMode.Hold : NoteMode.Tap;
        State.HoldNoteStart = null;
        State.HoldNoteLane = null;
        UpdateNoteModeLabel();
        GD.Print($"Note mode: {State.NoteMode}");
    }

    public void ToggleTripletMode() {
        State.IsTriplet = !State.IsTriplet;
        _statusLabel.Text = State.IsTriplet ? "Triplet grid: ON" : "Triplet grid: OFF";
        UpdateNoteModeLabel();
        UpdateInfoDisplay();
        GetTree().CreateTimer(1.5).Timeout += () => _statusLabel.Text = "";
        GD.Print($"Triplet grid toggled: {State.IsTriplet}");
    }

    public void SaveChart() => EditorChartIO.SaveChart(State, Chart, _statusLabel, this, OnSaveChartFileSelected);
    
    private void OnSaveChartFileSelected(string path) => 
        EditorChartIO.OnSaveChartFileSelected(path, State, Chart, _statusLabel, GetTree());

    public void LoadChart() => EditorChartIO.LoadChart(State, this, OnLoadChartFileSelected);
    
    private void OnLoadChartFileSelected(string path) => 
        EditorChartIO.OnLoadChartFileSelected(path, State, ref Chart, _statusLabel, GetTree(), UpdateInfoDisplay, QueueRedraw);
}

