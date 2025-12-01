using System;
using System.Collections.Generic;
using Godot;
using SYNK33.chart;
using SYNK33.core;

namespace SYNK33.editor;

public enum NoteMode {
    Tap,
    Hold
}

public partial class Editor : Control {
    [Export] public required Chart Chart;
    private AudioStreamPlayer? _audioStreamPlayer;
    private AudioStreamPlayer? _hitSoundPlayer;
    private AudioStreamPlayer? _holdSoundPlayer;
    private NoteTime _selectedTime = new(0, 0 ,0);
    private Label _selectedTimeLabel;
    private Label _selectedLaneLabel;
    private int _selectedLane;

    private int _laneWidth = 100;
    private const int MaxLanes = 3;
    private bool _isPlaying = false;
    private int _offsetX = 1920 / 2;
    private int _division = 1;
    private int _snapping = 4; // 1=whole note, 2=half, 4=quarter, 8=eighth, 16=sixteenth
    private float _zoom = 100;
    private float _panY = 1;

    private bool _isDragging = false;
    private Vector2 _dragStartMouse;
    private float _dragStartPan;
    
    private string _currentChartPath = "";
    private Label _statusLabel;
    
    // Note mode and hold note tracking
    private NoteMode _noteMode = NoteMode.Tap;
    private NoteTime? _holdNoteStart = null;
    private NoteType? _holdNoteLane = null;
    
    // Info box labels
    private Label _designerLabel;
    private Label _difficultyLabel;
    private Label _levelLabel;
    private Label _bpmLabel;
    private Label _beatsPerMeasureLabel;
    private Label _noteModeLabel;
    private Label _notesCountLabel;
    
    // Track which notes have been played during this playback session
    // Using a tuple of (Bar, Beat, Sixteenth, Lane) to uniquely identify notes
    private HashSet<(int, int, double, NoteType)> _playedNotes = new();
    
    // Track which hold notes are currently being played
    private HashSet<(int, int, double, NoteType)> _activeHoldNotes = new();

    public override void _Ready() {
        base._Ready();
        // TODO: load song from file
        _audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        _selectedTimeLabel = GetNode<Label>("%SelectedBeat");
        _selectedLaneLabel = GetNode<Label>("%SelectedLane");
        _statusLabel = GetNode<Label>("%StatusLabel");
        
        // Get info box labels
        _designerLabel = GetNode<Label>("%Designer");
        _difficultyLabel = GetNode<Label>("%Difficulty");
        _levelLabel = GetNode<Label>("%Level");
        _bpmLabel = GetNode<Label>("%BPM");
        _beatsPerMeasureLabel = GetNode<Label>("%BeatsPerMeasure");
        _noteModeLabel = GetNode<Label>("%NoteMode");
        _notesCountLabel = GetNode<Label>("%NotesCount");
        
        // Create hit sound player with polyphony for multiple notes
        _hitSoundPlayer = new AudioStreamPlayer();
        _hitSoundPlayer.MaxPolyphony = 8; // Allow multiple notes to play at once
        AddChild(_hitSoundPlayer);
        
        // Create hold sound player for continuous hold note sound
        _holdSoundPlayer = new AudioStreamPlayer();
        _holdSoundPlayer.MaxPolyphony = 3; // One per lane
        AddChild(_holdSoundPlayer);
        
        // Create a simple click/metronome sound
        CreateHitSound();
        CreateHoldSound();
        
        // Update info display
        UpdateInfoDisplay();
        
        GrabFocus();
    }
    
    private void CreateHitSound() {
        // Create a simple sine wave beep sound
        var sampleHz = 44100.0;
        var pulseSineHz = 800.0; // Frequency of the beep
        var duration = 0.05; // 50ms click
        
        var audioStream = new AudioStreamWav();
        audioStream.Format = AudioStreamWav.FormatEnum.Format16Bits;
        audioStream.MixRate = (int)sampleHz;
        audioStream.Stereo = false;
        
        var dataLength = (int)(duration * sampleHz);
        var data = new byte[dataLength * 2]; // 16-bit = 2 bytes per sample
        
        for (int i = 0; i < dataLength; i++) {
            var value = Math.Sin(2.0 * Math.PI * pulseSineHz * i / sampleHz);
            // Apply envelope to avoid clicks
            var envelope = Math.Exp(-5.0 * i / dataLength); // Fast decay
            var sample = (short)(value * envelope * 32767 * 0.5); // 50% amplitude
            
            // Convert to bytes (little-endian)
            data[i * 2] = (byte)(sample & 0xFF);
            data[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }
        
        audioStream.Data = data;
        _hitSoundPlayer.Stream = audioStream;
        _hitSoundPlayer.VolumeDb = 0; // Normal volume
    }
    
    private void CreateHoldSound() {
        // Create a subtle continuous tone for hold notes
        var sampleHz = 44100.0;
        var pulseHz = 400.0; // Lower frequency for hold sound
        var duration = 1.0; // 1 second loop
        
        var audioStream = new AudioStreamWav();
        audioStream.Format = AudioStreamWav.FormatEnum.Format16Bits;
        audioStream.MixRate = (int)sampleHz;
        audioStream.Stereo = false;
        audioStream.LoopMode = AudioStreamWav.LoopModeEnum.Forward; // Enable looping
        audioStream.LoopBegin = 0;
        audioStream.LoopEnd = (int)(duration * sampleHz);
        
        var dataLength = (int)(duration * sampleHz);
        var data = new byte[dataLength * 2];
        
        for (int i = 0; i < dataLength; i++) {
            // Create a smooth sine wave
            var value = Math.Sin(2.0 * Math.PI * pulseHz * i / sampleHz);
            // Keep it subtle with low amplitude
            var sample = (short)(value * 32767 * 0.15); // 15% amplitude for subtlety
            
            data[i * 2] = (byte)(sample & 0xFF);
            data[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }
        
        audioStream.Data = data;
        _holdSoundPlayer.Stream = audioStream;
        _holdSoundPlayer.VolumeDb = -12; // Quieter than tap notes
    }
    
    private void UpdateInfoDisplay() {
        _designerLabel.Text = $"Designer: {Chart.Designer}";
        _difficultyLabel.Text = $"Difficulty: {Chart.Difficulty}";
        _levelLabel.Text = $"Level: {Chart.Level}";
        _bpmLabel.Text = $"BPM: {Chart.Bpm}";
        _beatsPerMeasureLabel.Text = $"Beats/Measure: {Chart.BeatsPerMeasure}";
        _notesCountLabel.Text = $"Notes: {Chart.Notes.Count}";
        UpdateNoteModeLabel();
    }
    
    private void UpdateNoteModeLabel() {
        string modeText = _noteMode switch {
            NoteMode.Tap => "Mode: Tap (T)",
            NoteMode.Hold => _holdNoteStart == null 
                ? "Mode: Hold (T) - Click start" 
                : "Mode: Hold (T) - Click end",
            _ => "Mode: Unknown"
        };
        _noteModeLabel.Text = modeText;
    }

    public override void _Process(double delta) {
        base._Process(delta);
        // if (_isPlaying) {
        QueueRedraw();
        // }

        SelectBeat();
        SelectLane();
        
        // Check for notes to play during preview
        if (_isPlaying) {
            CheckAndPlayNoteHits();
        }
    }

    private void SelectBeat() {
        var beatTime = -(GetViewport().GetMousePosition().Y - _panY) / _zoom;
        
        // Prevent negative beats
        if (beatTime < 0) {
            beatTime = 0;
        }
        
        var bar = Math.Floor((beatTime) / Chart.BeatsPerMeasure);
        
        // Calculate snapped sixteenth based on _snapping
        // _snapping represents subdivisions per bar (1=whole, 2=half, 4=quarter, 8=eighth, 16=sixteenth)
        // We need to convert to sixteenth notes (where 4 sixteenths = 1 beat)
        var totalSixteenthsPerBar = Chart.BeatsPerMeasure * 4.0; // Total sixteenths in a bar
        var snapGridSize = totalSixteenthsPerBar / _snapping; // Size of each snap grid in sixteenths
        
        // Calculate total position in sixteenths from start of song
        var totalSixteenths = beatTime * 4.0;
        // Snap to grid
        var snappedTotalSixteenths = Math.Round(totalSixteenths / snapGridSize) * snapGridSize;
        
        // Convert back to bar/beat/sixteenth
        bar = Math.Floor(snappedTotalSixteenths / totalSixteenthsPerBar);
        var remainingSixteenths = snappedTotalSixteenths % totalSixteenthsPerBar;
        var beat = Math.Floor(remainingSixteenths / 4.0);
        var sixteenth = remainingSixteenths % 4.0;
        
        _selectedTimeLabel.Text = $"{bar + 1}.{beat+1}.{sixteenth + 1} (snap:{_snapping})";
        _selectedTime = new NoteTime((int)bar, (int)beat, sixteenth);
    }

    private void SelectLane() {
        var mouseX = GetViewport().GetMousePosition().X;
        var totalLaneWidth = MaxLanes * _laneWidth;
        var startX = (Size.X - totalLaneWidth) / 2;
        var relativeX = mouseX - startX;
        
        // Clamp to valid lanes (0 to MaxLanes-1)
        var oldLane = _selectedLane;
        _selectedLane = (int)Math.Clamp(Math.Floor(relativeX / _laneWidth), 0, MaxLanes - 1);
        if (oldLane != _selectedLane) {
            GD.Print($"Lane selected: {_selectedLane} (mouseX: {mouseX}, startX: {startX}, relativeX: {relativeX})");
        }
        _selectedLaneLabel.Text = "Lane: " + _selectedLane;
    }

    private void CheckAndPlayNoteHits() {
        if (_audioStreamPlayer == null || _hitSoundPlayer == null || _holdSoundPlayer == null) return;
        if (!_audioStreamPlayer.Playing) return;
        
        // Get current playhead position in seconds
        var songPosition = _audioStreamPlayer.GetPlaybackPosition() +
            AudioServer.GetTimeSinceLastMix() - AudioServer.GetOutputLatency();
        
        // Convert to beats
        var secondsPerBeat = 60.0f / Chart.Bpm;
        
        // Check each note
        for (int i = 0; i < Chart.Notes.Count; i++) {
            var note = Chart.Notes[i];
            
            // Create unique identifier for this note
            var noteId = (note.Bar, note.Beat, note.Sixteenth, note.Type);
            
            // Calculate note time in seconds
            var noteTimeInSeconds = (note.Bar * Chart.BeatsPerMeasure + note.Beat + note.Sixteenth / 4.0) * secondsPerBeat;
            
            // Check if it's a hold note
            bool isHoldNote = note.EndBar != 0 || note.EndBeat != 0 || note.EndSixteenth != 0;
            
            if (isHoldNote) {
                // Calculate end time
                var endTimeInSeconds = (note.EndBar * Chart.BeatsPerMeasure + note.EndBeat + note.EndSixteenth / 4.0) * secondsPerBeat;
                
                // Check if we're currently in the hold note duration
                if (songPosition >= noteTimeInSeconds && songPosition <= endTimeInSeconds) {
                    // Start hold sound if not already playing
                    if (!_activeHoldNotes.Contains(noteId)) {
                        _holdSoundPlayer.Play();
                        _activeHoldNotes.Add(noteId);
                    }
                } else {
                    // Stop hold sound if it was playing
                    if (_activeHoldNotes.Contains(noteId)) {
                        _activeHoldNotes.Remove(noteId);
                        // Only stop if no other hold notes are active
                        if (_activeHoldNotes.Count == 0) {
                            _holdSoundPlayer.Stop();
                        }
                    }
                }
                
                // Play hit sound at the start of hold note
                var timeDiff = songPosition - noteTimeInSeconds;
                var hitWindow = 0.1;
                if (timeDiff >= 0 && timeDiff <= hitWindow && !_playedNotes.Contains(noteId)) {
                    _hitSoundPlayer.Play();
                    _playedNotes.Add(noteId);
                }
            } else {
                // Tap note - just play once
                var timeDiff = songPosition - noteTimeInSeconds;
                var hitWindow = 0.1;
                
                if (timeDiff >= 0 && timeDiff <= hitWindow && !_playedNotes.Contains(noteId)) {
                    _hitSoundPlayer.Play();
                    _playedNotes.Add(noteId);
                }
            }
        }
    }

    public override void _Draw() {
        base._Draw();
        // TODO: draw it upwards (in reverse) so that everything moves up not down
        var totalLaneWidth = MaxLanes * _laneWidth;
        var startX = (Size.X - totalLaneWidth) / 2;
        
        // Draw lane boundaries (4 lines for 3 lanes)
        for (int i = 0; i <= MaxLanes; i++) {
            DrawLine(
                new Vector2(startX + i * _laneWidth, 0), 
                new Vector2(startX + i * _laneWidth, Size.Y), 
                new Color(255, 255, 255)
            );
        }

        var songLength = _audioStreamPlayer?.Stream.GetLength() / 60 * Chart.Bpm / Chart.BeatsPerMeasure;
        for (var i = 0; i < songLength; i++) {
            DrawBeatLines(i);
            DrawBarLines(i);
        }

        DrawNotes();
        DrawPlayhead();
        DrawSelector();
    }

    private void DrawNotes() {
        var totalLaneWidth = MaxLanes * _laneWidth;
        var startX = (Size.X - totalLaneWidth) / 2;
        
        foreach (var godotNote in Chart.Notes) {
            // Map NoteType to lane index
            int laneIndex = godotNote.Type switch {
                NoteType.Left => 0,
                NoteType.Middle => 1,
                NoteType.Right => 2,
                _ => 1
            };
            
            // Calculate note position
            float noteX = startX + laneIndex * _laneWidth + _laneWidth / 2;
            float noteY = -godotNote.Bar * Chart.BeatsPerMeasure * _zoom + _panY 
                          - godotNote.Beat * _zoom 
                          - (float)godotNote.Sixteenth / 4 * _zoom;
            
            // Draw note as a square (can be changed to diamond later)
            Vector2 notePos = new Vector2(noteX, noteY);
            float noteSize = 20;
            
            // Choose color based on lane
            Color noteColor = godotNote.Type switch {
                NoteType.Left => Color.FromHsv(0, 0.8f, 1),    // Red
                NoteType.Middle => Color.FromHsv(120, 0.8f, 1), // Green
                NoteType.Right => Color.FromHsv(240, 0.8f, 1),  // Blue
                _ => Colors.White
            };
            
            // Draw square
            DrawRect(new Rect2(notePos.X - noteSize / 2, notePos.Y - noteSize / 2, noteSize, noteSize), noteColor);
            
            // If it's a hold note, draw a line to the end
            if (godotNote.EndBar != 0 || godotNote.EndBeat != 0 || godotNote.EndSixteenth != 0) {
                float endNoteY = -godotNote.EndBar * Chart.BeatsPerMeasure * _zoom + _panY 
                                 - godotNote.EndBeat * _zoom 
                                 - (float)godotNote.EndSixteenth / 4 * _zoom;
                DrawLine(new Vector2(noteX, noteY), new Vector2(noteX, endNoteY), noteColor, 4);
                // Draw end square
                DrawRect(new Rect2(noteX - noteSize / 2, endNoteY - noteSize / 2, noteSize, noteSize), noteColor);
            }
        }
    }

    private void DrawSelector() {
        var totalLaneWidth = MaxLanes * _laneWidth;
        var startX = (Size.X - totalLaneWidth) / 2;
        
        var selectorY = -_selectedTime.Bar * Chart.BeatsPerMeasure * _zoom + _panY 
                        - _selectedTime.Beat * _zoom 
                        - (float)_selectedTime.Sixteenth / 4 * _zoom;
        var selectorX = startX + (float)(_selectedLane * _laneWidth) + _laneWidth / 2;
        
        // If we're placing a hold note and have already set the start
        if (_noteMode == NoteMode.Hold && _holdNoteStart != null && _holdNoteLane != null) {
            // Draw the hold note start position
            var startY = -_holdNoteStart.Bar * Chart.BeatsPerMeasure * _zoom + _panY 
                         - _holdNoteStart.Beat * _zoom 
                         - (float)_holdNoteStart.Sixteenth / 4 * _zoom;
            var startX_lane = startX + (float)((int)_holdNoteLane * _laneWidth) + _laneWidth / 2;
            
            // Draw start circle
            DrawCircle(new Vector2(startX_lane, startY), 15, Color.FromHsv(60, 1, 1, 0.7f));
            
            // Draw preview line to current position if on same lane
            if (_selectedLane == (int)_holdNoteLane) {
                DrawLine(
                    new Vector2(startX_lane, startY), 
                    new Vector2(selectorX, selectorY), 
                    Color.FromHsv(60, 1, 1, 0.5f), 
                    3
                );
                // Draw end circle
                DrawCircle(new Vector2(selectorX, selectorY), 15, Color.FromHsv(60, 1, 1, 0.5f));
            }
        }
        else {
            // Normal selector
            var color = _noteMode == NoteMode.Tap 
                ? Color.FromHsv(120, 1, 1, 0.5f)  // Green for tap
                : Color.FromHsv(60, 1, 1, 0.5f);  // Yellow for hold
            
            DrawCircle(new Vector2(selectorX, selectorY), 15, color);
        }
    }

    public override void _GuiInput(InputEvent @event) {
        base._GuiInput(@event);
        if (@event is InputEventMouseButton mouseEvent) {
            switch (mouseEvent.ButtonIndex) {
                case MouseButton.Left:
                    if (mouseEvent.Pressed) AddNote();
                    break;
                case MouseButton.Right:
                    if (mouseEvent.Pressed) RemoveNote();
                    break;
                case MouseButton.WheelUp:
                    ZoomAtMouse(_zoom * 1.1f, mouseEvent.Position);
                    break;
                case MouseButton.WheelDown:
                    ZoomAtMouse(_zoom / 1.1f, mouseEvent.Position);
                    break;
                case MouseButton.Middle:
                    TogglePanView(mouseEvent);
                    break;
            }
        }

        if (@event is InputEventMouseMotion mouseMotionEvent && _isDragging) {
            PanView(mouseMotionEvent);
        }
        if (@event is not InputEventKey keyEvent) return;
        
        if (keyEvent.IsActionPressed("editor_play")) {
            _isPlaying = !_isPlaying;
            if (_isPlaying)
                PlayPreview();
            else
                StopPreview();
        }
        
        // Save and Load shortcuts
        if (keyEvent.Pressed && !keyEvent.Echo) {
            if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.S) {
                SaveChart();
            }
            else if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.O) {
                LoadChart();
            }
            else if (keyEvent.Keycode == Key.T) {
                // Toggle note mode
                _noteMode = _noteMode == NoteMode.Tap ? NoteMode.Hold : NoteMode.Tap;
                _holdNoteStart = null; // Reset hold note placement
                _holdNoteLane = null;
                UpdateNoteModeLabel();
                GD.Print($"Note mode: {_noteMode}");
            }
        }
        
        // Snapping controls
        if (keyEvent.Pressed && !keyEvent.Echo) {
            if (keyEvent.Keycode == Key.Key1) {
                _snapping = 1;
                GD.Print($"Snapping set to: {_snapping} (whole notes)");
            }
            else if (keyEvent.Keycode == Key.Key2) {
                _snapping = 2;
                GD.Print($"Snapping set to: {_snapping} (half notes)");
            }
            else if (keyEvent.Keycode == Key.Key3) {
                _snapping = 4;
                GD.Print($"Snapping set to: {_snapping} (quarter notes)");
            }
            else if (keyEvent.Keycode == Key.Key4) {
                _snapping = 8;
                GD.Print($"Snapping set to: {_snapping} (eighth notes)");
            }
            else if (keyEvent.Keycode == Key.Key5) {
                _snapping = 16;
                GD.Print($"Snapping set to: {_snapping} (sixteenth notes)");
            }
            else if (keyEvent.Keycode == Key.Equal || keyEvent.Keycode == Key.Plus) {
                _snapping = Math.Min(_snapping * 2, 64);
                GD.Print($"Snapping increased to: {_snapping}");
            }
            else if (keyEvent.Keycode == Key.Minus) {
                _snapping = Math.Max(_snapping / 2, 1);
                GD.Print($"Snapping decreased to: {_snapping}");
            }
        }
    }

    private void PanView(InputEventMouseMotion mouseMotionEvent) {
        // TODO: clamp pan value
        var deltaY = mouseMotionEvent.Position.Y - _dragStartMouse.Y;
        _panY = _dragStartPan + deltaY;
        QueueRedraw();
    }

    private void TogglePanView(InputEventMouseButton mouseEventPressed) {
        if (mouseEventPressed.Pressed) {
            _isDragging =  true;
            _dragStartMouse = mouseEventPressed.Position;
            _dragStartPan = _panY;
        }
        else {
            _isDragging = false;
        }
    }

    private void ZoomAtMouse(float newZoom, Vector2 mouseEventPosition) {
        var oldZoom = _zoom;
        _zoom = float.Clamp(newZoom, 10.0f, 500.0f);
        var previousPos = (mouseEventPosition.Y - _panY) / oldZoom;
        _panY = mouseEventPosition.Y - previousPos * _zoom;
        GD.Print($"Zoom changed: {oldZoom:F1} -> {_zoom:F1}");
        QueueRedraw();
    }

    private void RemoveNote() {
        // Map lane index to NoteType (0=Left, 1=Middle, 2=Right)
        NoteType noteType = _selectedLane switch {
            0 => NoteType.Left,
            1 => NoteType.Middle,
            2 => NoteType.Right,
            _ => NoteType.Middle
        };
        
        GD.Print($"Attempting to remove note at Bar:{_selectedTime.Bar} Beat:{_selectedTime.Beat} Sixteenth:{_selectedTime.Sixteenth} Lane:{noteType}");
        
        // Find and remove note at the selected time and lane
        for (int i = Chart.Notes.Count - 1; i >= 0; i--) {
            var note = Chart.Notes[i];
            if (note.Bar == _selectedTime.Bar && 
                note.Beat == _selectedTime.Beat && 
                Math.Abs(note.Sixteenth - _selectedTime.Sixteenth) < 0.01 &&
                note.Type == noteType) {
                GD.Print($"Note removed at index {i}: Bar:{note.Bar} Beat:{note.Beat} Sixteenth:{note.Sixteenth} Type:{note.Type}");
                Chart.Notes.RemoveAt(i);
                UpdateInfoDisplay();
                QueueRedraw();
                break;
            }
        }
        GD.Print($"Total notes in chart: {Chart.Notes.Count}");
    }

    private void AddNote() {
        // Map lane index to NoteType (0=Left, 1=Middle, 2=Right)
        NoteType noteType = _selectedLane switch {
            0 => NoteType.Left,
            1 => NoteType.Middle,
            2 => NoteType.Right,
            _ => NoteType.Middle
        };
        
        if (_noteMode == NoteMode.Tap) {
            // Check if a note already exists at this position
            foreach (var existingNote in Chart.Notes) {
                if (existingNote.Bar == _selectedTime.Bar && 
                    existingNote.Beat == _selectedTime.Beat && 
                    Math.Abs(existingNote.Sixteenth - _selectedTime.Sixteenth) < 0.01 &&
                    existingNote.Type == noteType) {
                    GD.Print($"Note already exists at Bar:{_selectedTime.Bar} Beat:{_selectedTime.Beat} Sixteenth:{_selectedTime.Sixteenth} Lane:{noteType}");
                    return;
                }
            }
            
            GD.Print($"Adding tap note at Bar:{_selectedTime.Bar} Beat:{_selectedTime.Beat} Sixteenth:{_selectedTime.Sixteenth} Lane:{noteType}");
            
            // Create a new Tap note at the selected time and lane
            var newNote = new Note.Tap(_selectedTime, noteType);
            
            // Convert to GodotNote and add to chart
            var godotNote = new GodotNote(newNote);
            Chart.Notes.Add(godotNote);
            
            // If preview is playing, make sure the note can be played
            if (_isPlaying && _audioStreamPlayer != null) {
                var noteId = (_selectedTime.Bar, _selectedTime.Beat, _selectedTime.Sixteenth, noteType);
                _playedNotes.Remove(noteId);
            }
            
            UpdateInfoDisplay();
            QueueRedraw();
        }
        else if (_noteMode == NoteMode.Hold) {
            // Two-click placement for hold notes
            if (_holdNoteStart == null) {
                // First click - set start position
                _holdNoteStart = _selectedTime;
                _holdNoteLane = noteType;
                UpdateNoteModeLabel();
                GD.Print($"Hold note start set at Bar:{_selectedTime.Bar} Beat:{_selectedTime.Beat} Sixteenth:{_selectedTime.Sixteenth} Lane:{noteType}");
            }
            else {
                // Second click - set end position and create note
                // Ensure we're on the same lane
                if (noteType != _holdNoteLane) {
                    GD.Print($"Hold note must be on the same lane! Start was on {_holdNoteLane}, but end is on {noteType}");
                    _holdNoteStart = null;
                    _holdNoteLane = null;
                    UpdateNoteModeLabel();
                    return;
                }
                
                // Ensure end is after start
                var startTotal = _holdNoteStart.Bar * Chart.BeatsPerMeasure * 4 + _holdNoteStart.Beat * 4 + _holdNoteStart.Sixteenth;
                var endTotal = _selectedTime.Bar * Chart.BeatsPerMeasure * 4 + _selectedTime.Beat * 4 + _selectedTime.Sixteenth;
                
                if (endTotal <= startTotal) {
                    GD.Print($"Hold note end must be after start!");
                    _holdNoteStart = null;
                    _holdNoteLane = null;
                    UpdateNoteModeLabel();
                    return;
                }
                
                GD.Print($"Adding hold note from Bar:{_holdNoteStart.Bar} Beat:{_holdNoteStart.Beat} to Bar:{_selectedTime.Bar} Beat:{_selectedTime.Beat} Lane:{noteType}");
                
                // Create hold note
                var newNote = new Note.Hold(_holdNoteStart, _selectedTime, noteType);
                var godotNote = new GodotNote(newNote);
                Chart.Notes.Add(godotNote);
                
                // If preview is playing, make sure the note can be played
                if (_isPlaying && _audioStreamPlayer != null) {
                    var noteId = (_holdNoteStart.Bar, _holdNoteStart.Beat, _holdNoteStart.Sixteenth, noteType);
                    _playedNotes.Remove(noteId);
                    _activeHoldNotes.Remove(noteId);
                }
                
                // Reset hold note placement
                _holdNoteStart = null;
                _holdNoteLane = null;
                UpdateNoteModeLabel();
                UpdateInfoDisplay();
                QueueRedraw();
            }
        }
    }

    private void DrawPlayhead() {
        if (_isPlaying) {
            var songPosition = _audioStreamPlayer?.GetPlaybackPosition() +
                AudioServer.GetTimeSinceLastMix() - AudioServer.GetOutputLatency();
            var playPosition = -(float)songPosition / 60 * Chart.Bpm * _zoom + _panY;
            DrawLine(
                new Vector2(0,playPosition), 
                new Vector2(Size.X, playPosition), 
                new Color(255, 0, 0)
            );
        }
    }

    private void DrawBarLines(int i) {
        var size2 = -i * _zoom * Chart.BeatsPerMeasure + _panY;
        DrawLine(
            new Vector2(0, size2), 
            new Vector2(Size.X, size2), 
            new Color(255, 255, 255)
        );
    }

    private void DrawBeatLines(int i) {
        // Draw subdivision lines based on current snapping
        // This creates a grid that shows where notes will snap
        var totalSixteenthsPerBar = Chart.BeatsPerMeasure * 4.0;
        var snapGridSize = totalSixteenthsPerBar / _snapping;

        // Draw a line for each snap position
        for (var j = 0; j < _snapping; j++) {
            // Skip the bar line (j == 0) as it's drawn by DrawBarLines
            if (j == 0) continue;

            // Calculate position in sixteenths
            var sixteenthPosition = j * snapGridSize;
            var beatPosition = sixteenthPosition / 4.0;

            // Determine line brightness based on musical subdivision
            // Beat lines (every 4 sixteenths) are brighter
            var isBeatLine = Math.Abs(sixteenthPosition % 4.0) < 0.01;
            var color = isBeatLine
                ? new Color(0.6f, 0.6f, 0.6f)  // Brighter for beat lines
                : new Color(0.4f, 0.4f, 0.4f); // Dimmer for subdivision lines

            var size = (float)(-i * _zoom * Chart.BeatsPerMeasure + -beatPosition * _zoom + _panY);
            DrawLine(
                new Vector2(0, size), 
                new Vector2(Size.X, size), 
                color
            );
        }
    }

    private void PlayPreview() {
        var seekTime = _selectedTime.ToMilliseconds(Chart.BeatsPerMeasure, 60.0f / Chart.Bpm);
        GD.Print($"Playing preview from Bar:{_selectedTime.Bar} Beat:{_selectedTime.Beat} ({seekTime:F2}s)");
        
        // Reset played notes tracking
        _playedNotes.Clear();
        
        if (_audioStreamPlayer != null) {
            _audioStreamPlayer.Play();
            _audioStreamPlayer.Seek(seekTime);
        }
    }

    private void StopPreview() {
        GD.Print("Stopping preview");
        _audioStreamPlayer?.Stop();
        _holdSoundPlayer?.Stop();
        
        // Reset played notes tracking
        _playedNotes.Clear();
        _activeHoldNotes.Clear();
    }

    private void SaveChart() {
        FileDialog fileDialog = new FileDialog {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Filters = new string[] { "*.tres ; Godot Resource File" },
            Title = "Save Chart"
        };
        
        // Set initial path if we have one
        if (!string.IsNullOrEmpty(_currentChartPath)) {
            fileDialog.CurrentPath = _currentChartPath;
        } else {
            fileDialog.CurrentDir = "res://assets/charts/";
        }
        
        fileDialog.FileSelected += OnSaveChartFileSelected;
        AddChild(fileDialog);
        fileDialog.PopupCentered(new Vector2I(800, 600));
        GD.Print("Save dialog opened");
    }

    private void OnSaveChartFileSelected(string path) {
        GD.Print($"Saving chart to: {path}");
        
        var error = ResourceSaver.Save(Chart, path);
        
        if (error == Error.Ok) {
            _currentChartPath = path;
            _statusLabel.Text = $"Chart saved: {System.IO.Path.GetFileName(path)}";
            GD.Print($"Chart saved successfully to {path}");
        } else {
            _statusLabel.Text = $"Save failed: {error}";
            GD.PrintErr($"Failed to save chart: {error}");
        }
        
        // Auto-hide status after 3 seconds
        GetTree().CreateTimer(3.0).Timeout += () => _statusLabel.Text = "";
    }

    private void LoadChart() {
        FileDialog fileDialog = new FileDialog {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Filters = new string[] { "*.tres ; Godot Resource File" },
            Title = "Load Chart"
        };
        
        // Set initial path
        if (!string.IsNullOrEmpty(_currentChartPath)) {
            fileDialog.CurrentPath = _currentChartPath;
        } else {
            fileDialog.CurrentDir = "res://assets/charts/";
        }
        
        fileDialog.FileSelected += OnLoadChartFileSelected;
        AddChild(fileDialog);
        fileDialog.PopupCentered(new Vector2I(800, 600));
        GD.Print("Load dialog opened");
    }

    private void OnLoadChartFileSelected(string path) {
        GD.Print($"Loading chart from: {path}");
        
        var loadedChart = ResourceLoader.Load<Chart>(path);
        
        if (loadedChart != null) {
            Chart = loadedChart;
            _currentChartPath = path;
            _statusLabel.Text = $"Chart loaded: {System.IO.Path.GetFileName(path)} ({Chart.Notes.Count} notes)";
            GD.Print($"Chart loaded successfully from {path} with {Chart.Notes.Count} notes");
            UpdateInfoDisplay();
            QueueRedraw();
        } else {
            _statusLabel.Text = "Load failed: Invalid chart file";
            GD.PrintErr($"Failed to load chart from {path}");
        }
        
        // Auto-hide status after 3 seconds
        GetTree().CreateTimer(3.0).Timeout += () => _statusLabel.Text = "";
    }
}