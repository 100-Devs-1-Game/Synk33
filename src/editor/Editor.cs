using System;
using Godot;
using SYNK33.chart;
using SYNK33.core;

namespace SYNK33.editor;

public partial class Editor : Control {
    [Export] public required Chart Chart;
    private AudioStreamPlayer? _audioStreamPlayer;
    private NoteTime _selectedTime = new(0, 0 ,0);
    private Label _selectedTimeLabel;
    private Label _selectedLaneLabel;
    private double _selectedLane;

    private int _laneWidth = 100;
    private bool _isPlaying = false;
    private int _offsetX = 1920 / 2;
    private int _division = 1;
    private float _zoom = 100;
    private float _panY = 1;

    private bool _isDragging = false;
    private Vector2 _dragStartMouse;
    private float _dragStartPan;

    public override void _Ready() {
        base._Ready();
        // TODO: load song from file
        _audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        _selectedTimeLabel = GetNode<Label>("%SelectedBeat");
        _selectedLaneLabel = GetNode<Label>("%SelectedLane");
        GrabFocus();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        // if (_isPlaying) {
        QueueRedraw();
        // }

        SelectBeat();
        SelectLane();
    }

    private void SelectBeat() {
        var beatTime = -(GetViewport().GetMousePosition().Y - _panY) / _zoom;
        var bar = Math.Floor((beatTime) / Chart.BeatsPerMeasure);
        var beat = Math.Floor((beatTime) % Chart.BeatsPerMeasure);
        var sixteenth = Math.Floor((beatTime) % Chart.BeatsPerMeasure * 4) % 4;
        _selectedTimeLabel.Text = $"{bar + 1}.{beat+1}.{sixteenth + 1}";
        _selectedTime = new NoteTime((int)bar, (int)beat, sixteenth);
    }

    private void SelectLane() {
        var mouseX = GetViewport().GetMousePosition().X;
        _selectedLane = Math.Floor(mouseX / _laneWidth);
        _selectedLaneLabel.Text = "Lane: " + _selectedLane;
    }

    public override void _Draw() {
        base._Draw();
        // TODO: draw it upwards (in reverse) so that everything moves up not down
        for (int i = 0; i < 3; i++) {
            DrawLine(
                new Vector2(_offsetX + i * _laneWidth, 0), 
                new Vector2(_offsetX + i * _laneWidth, Size.Y), 
                new Color(255, 255, 255)
            );
        }

        var songLength = _audioStreamPlayer?.Stream.GetLength() / 60 * Chart.Bpm / Chart.BeatsPerMeasure;
        for (var i = 0; i < songLength; i++) {
            DrawBeatLines(i);
            DrawBarLines(i);
        }

        DrawPlayhead();
        DrawSelector();
    }

    private void DrawSelector() {
        DrawCircle(
            new Vector2(
                (float)(_selectedLane * _laneWidth), 
                -_selectedTime.Bar * Chart.BeatsPerMeasure * _zoom + _panY 
                - _selectedTime.Beat * _zoom -
                (float)_selectedTime.Sixteenth / 4  * _zoom
            ), 
            15, 
            Color.FromHsv(120, 1, 1, 0.5f)
        );
    }

    public override void _GuiInput(InputEvent @event) {
        base._GuiInput(@event);
        if (@event is InputEventMouseButton mouseEvent) {
            switch (mouseEvent.ButtonIndex) {
                case MouseButton.Left:
                    AddNote();
                    break;
                case MouseButton.Right:
                    RemoveNote();
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
        QueueRedraw();
    }

    private void RemoveNote() {
        throw new NotImplementedException();
    }

    private void AddNote() {
        throw new NotImplementedException();
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
        for (var j = 0; j < Chart.BeatsPerMeasure * _division; j++) {
            var color = new Color(0.411765f, 0.411765f, 0.411765f);
            var size = -i * _zoom * Chart.BeatsPerMeasure + -j * _zoom / _division + _panY;
            DrawLine(
                new Vector2(0, size), 
                new Vector2(Size.X, size), 
                color
            );
        }
    }

    private void PlayPreview() {
        _audioStreamPlayer?.Play();
        _audioStreamPlayer?.Seek(_selectedTime.ToMilliseconds(Chart.BeatsPerMeasure, 60.0f / Chart.Bpm));
    }

    private void StopPreview() {
        _audioStreamPlayer?.Stop();
    }
}