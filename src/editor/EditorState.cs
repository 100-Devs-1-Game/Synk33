using Godot;
using SYNK33.chart;

namespace SYNK33.editor;

public enum NoteMode { Tap, Hold }

public class EditorState {
    public NoteTime SelectedTime { get; set; } = new(0, 0, 0);
    public int SelectedLane { get; set; }
    public NoteMode NoteMode { get; set; } = NoteMode.Tap;
    public NoteTime? HoldNoteStart { get; set; }
    public NoteType? HoldNoteLane { get; set; }
    public int Snapping { get; set; } = 4;
    public bool IsTriplet { get; set; }
    
    public float Zoom { get; set; } = EditorConstants.DefaultZoom;
    public float PanY { get; set; } = 1;
    public bool IsDragging { get; set; }
    public Vector2 DragStartMouse { get; set; }
    public float DragStartPan { get; set; }
    
    public bool IsPlaying { get; set; }
    public string CurrentChartPath { get; set; } = "";
    
    public Vector2 MousePosition { get; set; }

    public int GetEffectiveSnapping() => IsTriplet ? Snapping * 3 : Snapping;

    public string GetSnapName() {
        var baseName = Snapping switch {
            1 => "Whole",
            2 => "Half",
            4 => "Quarter",
            8 => "Eighth",
            16 => "Sixteenth",
            _ => Snapping.ToString()
        };

        return IsTriplet 
            ? $"{baseName}-triplet ({GetEffectiveSnapping()})" 
            : $"{baseName} ({Snapping})";
    }
}

public static class EditorConstants {
    public const int MaxLanes = 3;
    public const int LaneWidth = 100;
    public const float DefaultZoom = 100f;
    public const float MinZoom = 10f;
    public const float MaxZoom = 500f;
    public const float LanePadding = 8f;
    public const float TapNoteHeight = 20f;
    public const float HoldNoteStartHeadHeight = 20f;
    public const float HoldNoteEndHeadHeight = 14f;
    
    // Waveform display constants
    public const float WaveformWidth = 140f;
    public const float WaveformMargin = 10f;
    public const float WaveformPadding = 8f;
}
