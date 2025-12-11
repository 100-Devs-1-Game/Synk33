using System;
using System.Collections.Generic;
using Godot;
using SYNK33.chart;

namespace SYNK33.editor;

public partial class Editor : Control {
    [Export] public required Chart Chart;
    [Export] public AudioStream? TapSound;
    [Export] public AudioStream? HoldSound;
    
    public EditorState State { get; private set; } = new();
    
    private AudioStreamPlayer? _audioStreamPlayer;
    private AudioStreamPlayer? _hitSoundPlayer;
    private AudioStreamPlayer? _holdSoundPlayer;
    
    private Label _selectedTimeLabel = null!;
    private Label _selectedLaneLabel = null!;
    private Label _statusLabel = null!;
    private Label _fileNameLabel = null!;
    private Label _designerLabel = null!;
    private Label _difficultyLabel = null!;
    private Label _levelLabel = null!;
    private Label _tempoModifierLabel = null!;
    private Label _bpmLabel = null!;
    private Label _beatsPerMeasureLabel = null!;
    private Label _songNameLabel = null!;
    private Label _songAuthorLabel = null!;
    private Label _songBpmLabel = null!;
    private Label _noteModeLabel = null!;
    private Label _notesCountLabel = null!;
    
    private MenuButton? _fileMenuButton;
    private Button? _noteModeButton;
    private Button? _tripletButton;
    private Button? _grid1Button;
    private Button? _grid2Button;
    private Button? _grid4Button;
    private Button? _grid8Button;
    private Button? _grid16Button;
    private Button? _grid32Button;
    private Button? _grid64Button;
    
    private readonly HashSet<(long, long, double, NoteType)> _playedNotes = [];
    private readonly HashSet<(long, long, double, NoteType)> _activeHoldNotes = [];
    
    private WaveformData? _waveformData;

    public override void _Ready() {
        base._Ready();
        InitializeAudioPlayers();
        InitializeUiElements();
        InitializeMenuBar();
        InitializeAudioSounds();

        TryAutoLoadLastChart();

        UpdateInfoDisplay();
        GrabFocus();
    }

    private void TryAutoLoadLastChart() {
        try {
            if (!FileAccess.FileExists("user://last_chart_path.txt")) return;
            using var fa = FileAccess.Open("user://last_chart_path.txt", FileAccess.ModeFlags.Read);
            var path = fa.GetAsText().Trim();
            fa.Close();
            if (string.IsNullOrEmpty(path)) return;

            GD.Print($"Attempting to auto-load last chart: {path}");
            var loadedChart = ResourceLoader.Load<Chart>(path);
            if (loadedChart == null) {
                GD.PrintErr($"Auto-load failed: could not load chart at {path}");
                return;
            }

            Chart = loadedChart;
            State.CurrentChartPath = path;

            // Load audio if available
            if (_audioStreamPlayer != null) {
                if (Chart.Song?.Audio != null) {
                    _audioStreamPlayer.Stream = Chart.Song.Audio;
                    GD.Print($"Auto-loaded audio from song: {Chart.Song.Name}");
                    
                    _waveformData = EditorWaveform.AnalyzeAudioStream(Chart.Song.Audio, (int)Size.Y);
                    GD.Print("Waveform analysis complete");
                } else {
                    _audioStreamPlayer.Stream = null;
                    _waveformData = null;
                }
            }

            // Apply a slight pan so the loaded chart is visible in the editor view
            // Use a fraction of the viewport height so behavior adapts to window size
            State.PanY = Size.Y * 0.5f;

             UpdateInfoDisplay();
             QueueRedraw();
             GD.Print($"Auto-loaded chart: {path}");
        } catch (Exception e) {
            GD.PrintErr($"Error while auto-loading last chart: {e.Message}");
        }
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
        _fileNameLabel = GetNode<Label>("%FileName");
        _designerLabel = GetNode<Label>("%Designer");
        _difficultyLabel = GetNode<Label>("%Difficulty");
        _levelLabel = GetNode<Label>("%Level");
        _tempoModifierLabel = GetNode<Label>("%TempoModifier");
        _bpmLabel = GetNode<Label>("%BPM");
        _beatsPerMeasureLabel = GetNode<Label>("%BeatsPerMeasure");
        _songNameLabel = GetNode<Label>("%SongName");
        _songAuthorLabel = GetNode<Label>("%SongAuthor");
        _songBpmLabel = GetNode<Label>("%SongBPM");
        _notesCountLabel = GetNode<Label>("%NotesCount");
        
        _noteModeButton = GetNode<Button>("%NoteModeButton");
        _noteModeButton.Pressed += OnNoteModeButtonPressed;
        
        _tripletButton = GetNode<Button>("%TripletButton");
        _tripletButton.Toggled += OnTripletButtonToggled;
        
        _grid1Button = GetNode<Button>("%Grid1Button");
        _grid1Button.Pressed += () => SetGridSize(1);
        
        _grid2Button = GetNode<Button>("%Grid2Button");
        _grid2Button.Pressed += () => SetGridSize(2);
        
        _grid4Button = GetNode<Button>("%Grid4Button");
        _grid4Button.Pressed += () => SetGridSize(4);
        
        _grid8Button = GetNode<Button>("%Grid8Button");
        _grid8Button.Pressed += () => SetGridSize(8);
        
        _grid16Button = GetNode<Button>("%Grid16Button");
        _grid16Button.Pressed += () => SetGridSize(16);
        
        _grid32Button = GetNode<Button>("%Grid32Button");
        _grid32Button.Pressed += () => SetGridSize(32);
        
        _grid64Button = GetNode<Button>("%Grid64Button");
        _grid64Button.Pressed += () => SetGridSize(64);
        
        UpdateGridButtons();
        UpdateNoteModeButton();
    }

    private void InitializeMenuBar() {
        _fileMenuButton = GetNode<MenuButton>("%FileMenu");
        
        var filePopup = _fileMenuButton.GetPopup();
        filePopup.Clear();
        filePopup.AddItem("Load Chart", 0);
        filePopup.AddItem("Save Chart", 1);
        
        filePopup.IndexPressed += OnFileMenuIndexPressed;
        
        GD.Print($"Menu initialized with {filePopup.ItemCount} items");
    }

    private void OnFileMenuIndexPressed(long index) {
        GD.Print($"Menu item pressed: index={index}");
        switch (index) {
            case 0:
                LoadChart();
                break;
            case 1:
                SaveChart();
                break;
            default:
                GD.Print($"Unknown menu index: {index}");
                break;
        }
    }

    private void OnNoteModeButtonPressed() {
        ToggleNoteMode();
    }

    private void OnTripletButtonToggled(bool pressed) {
        State.IsTriplet = pressed;
        _statusLabel.Text = State.IsTriplet ? "Triplet grid: ON" : "Triplet grid: OFF";
        UpdateNoteModeLabel();
        UpdateInfoDisplay();
        GetTree().CreateTimer(1.5).Timeout += () => _statusLabel.Text = "";
        GD.Print($"Triplet grid toggled: {State.IsTriplet}");
    }

    private void SetGridSize(int size) {
        State.Snapping = size;
        UpdateGridButtons();
        _statusLabel.Text = $"Grid: {State.GetSnapName()}";
        GetTree().CreateTimer(1.5).Timeout += () => _statusLabel.Text = "";
        GD.Print($"Grid size set to: {size} ({State.GetSnapName()})");
    }

    public void UpdateGridButtons() {
        _grid1Button!.Modulate = State.Snapping == 1 ? new Color(0.5f, 1f, 0.5f) : Colors.White;
        _grid2Button!.Modulate = State.Snapping == 2 ? new Color(0.5f, 1f, 0.5f) : Colors.White;
        _grid4Button!.Modulate = State.Snapping == 4 ? new Color(0.5f, 1f, 0.5f) : Colors.White;
        _grid8Button!.Modulate = State.Snapping == 8 ? new Color(0.5f, 1f, 0.5f) : Colors.White;
        _grid16Button!.Modulate = State.Snapping == 16 ? new Color(0.5f, 1f, 0.5f) : Colors.White;
        _grid32Button!.Modulate = State.Snapping == 32 ? new Color(0.5f, 1f, 0.5f) : Colors.White;
        _grid64Button!.Modulate = State.Snapping == 64 ? new Color(0.5f, 1f, 0.5f) : Colors.White;
    }

    private void UpdateNoteModeButton() {
        _noteModeButton!.Text = State.NoteMode == NoteMode.Tap ? "Tap (T)" : "Hold (T)";
    }

    private void InitializeAudioSounds() {
        GD.Print("Initializing editor audio sounds...");
        EditorAudio.CreateHitSound(_hitSoundPlayer!, TapSound);
        EditorAudio.CreateHoldSound(_holdSoundPlayer!, HoldSound);
        GD.Print($"TapSound provided: {TapSound != null}, HoldSound provided: {HoldSound != null}");
    }

    private void UpdateInfoDisplay() {
        // Update file name display
        if (string.IsNullOrEmpty(State.CurrentChartPath)) {
            _fileNameLabel.Text = "File: (not saved)";
        } else {
            var fileName = System.IO.Path.GetFileName(State.CurrentChartPath);
            _fileNameLabel.Text = $"File: {fileName}";
        }
        
        // Chart Info
        _designerLabel.Text = $"Designer: {Chart.Designer}";
        _levelLabel.Text = $"Level: {Chart.Level}";
        _tempoModifierLabel.Text = $"Tempo: {Chart.TempoModifier:F1}x";
        _beatsPerMeasureLabel.Text = $"Beats/Measure: {Chart.BeatsPerMeasure}";
        _notesCountLabel.Text = $"Notes: {Chart.Notes.Count}";
        
        // Song Info
        _songNameLabel.Text = $"Name: {Chart.Song.Name}";
        _songAuthorLabel.Text = $"Author: {Chart.Song.Author}";
        _songBpmLabel.Text = $"Base BPM: {Chart.Song.Bpm:F1}";
        _difficultyLabel.Text = $"Difficulty: {GetDifficultyString(Chart.Song.Difficulties)}";
        _bpmLabel.Text = $"Effective BPM: {Chart.Bpm:F1}";
    }
    
    private static string GetDifficultyString(int difficulties) {
        if (difficulties == 0) return "None";
        
        var diffList = new List<string>();
        if ((difficulties & (1 << 0)) != 0) diffList.Add("Easy");
        if ((difficulties & (1 << 1)) != 0) diffList.Add("Normal");
        if ((difficulties & (1 << 2)) != 0) diffList.Add("Hard");
        if ((difficulties & (1 << 3)) != 0) diffList.Add("Expert");
        
        return string.Join(", ", diffList);
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
            EditorPlayback.CheckAndPlayNoteHits(
                Chart,
                _audioStreamPlayer, 
                _hitSoundPlayer, 
                _holdSoundPlayer, 
                _playedNotes, 
                _activeHoldNotes
            );
        }
    }

    public override void _Draw() {
        base._Draw();
        const int totalLaneWidth = EditorConstants.MaxLanes * EditorConstants.LaneWidth;
        var startX = (Size.X - totalLaneWidth) / 2;
        
        EditorDrawing.DrawLaneBoundaries(this, startX);
        EditorDrawing.DrawGridLines(this, _audioStreamPlayer, Chart, State.GetEffectiveSnapping(), State.Zoom, State.PanY);
        EditorDrawing.DrawNotes(this, Chart, State, _audioStreamPlayer);
        EditorWaveform.DrawWaveform(this, _waveformData, Chart, State.Zoom, State.PanY, Size.X, Size.Y);
        EditorDrawing.DrawCursorLine(this, State);
        EditorDrawing.DrawPlayhead(this, _audioStreamPlayer, State.Zoom, State.PanY, Chart.Bpm);
        EditorDrawing.DrawSelector(this, Chart, State);
    }

    public override void _GuiInput(InputEvent @event) {
        base._GuiInput(@event);

        switch (@event) {
            case InputEventMouseButton mouseEvent:
                EditorInput.HandleMouseButton(mouseEvent, this);
                break;
            case InputEventMouseMotion mouseMotionEvent:
                State.MousePosition = mouseMotionEvent.Position;
                if (State.IsDragging) {
                    EditorView.PanView(State, mouseMotionEvent, QueueRedraw);
                }
                QueueRedraw(); // Redraw to update cursor line
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
        UpdateNoteModeButton();
        GD.Print($"Note mode: {State.NoteMode}");
    }

    public void ToggleTripletMode() {
        State.IsTriplet = !State.IsTriplet;
        _tripletButton!.SetPressedNoSignal(State.IsTriplet);
        _statusLabel.Text = State.IsTriplet ? "Triplet grid: ON" : "Triplet grid: OFF";
        UpdateInfoDisplay();
        GetTree().CreateTimer(1.5).Timeout += () => _statusLabel.Text = "";
        GD.Print($"Triplet grid toggled: {State.IsTriplet}");
    }

    public void SaveChart() => EditorChartIO.SaveChart(State, this, OnSaveChartFileSelected);
    
    private void OnSaveChartFileSelected(string path) => 
        EditorChartIO.OnSaveChartFileSelected(path, State, Chart, _statusLabel, GetTree());

    public void LoadChart() => EditorChartIO.LoadChart(State, this, OnLoadChartFileSelected);
    
    private void OnLoadChartFileSelected(string path) {
        EditorChartIO.OnLoadChartFileSelected(path, State, ref Chart, _statusLabel, GetTree(), UpdateInfoDisplay, QueueRedraw, _audioStreamPlayer);
        State.PanY = Size.Y * 0.5f;
        
        // Generate waveform data if audio is loaded
        if (Chart.Song?.Audio != null) {
            _waveformData = EditorWaveform.AnalyzeAudioStream(Chart.Song.Audio, (int)Size.Y);
            GD.Print("Waveform analysis complete");
        } else {
            _waveformData = null;
        }
        
        QueueRedraw();
    }
}
