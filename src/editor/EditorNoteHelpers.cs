using System;
using System.Linq;
using Godot;
using SYNK33.chart;

namespace SYNK33.editor;

public static class EditorNoteHelpers {
    public static int GetLaneIndex(NoteType type) => type switch {
        NoteType.Left => 0,
        NoteType.Middle => 1,
        NoteType.Right => 2,
        _ => 1
    };

    public static NoteType GetNoteTypeFromLane(int lane) => lane switch {
        0 => NoteType.Left,
        1 => NoteType.Middle,
        2 => NoteType.Right,
        _ => NoteType.Middle
    };

    public static float GetHueForLane(NoteType type) => type switch {
        NoteType.Left => 270f,
        NoteType.Middle => 210f,
        NoteType.Right => 30f,
        _ => 210f
    };

    public static Color GetLaneColor(NoteType type) {
        var hue = GetHueForLane(type);
        return Color.FromHsv(hue, 0.8f, 1f);
    }

    public static float CalculateNoteYPosition(
        int bar,
        int beat,
        double sixteenth,
        Chart chart,
        float zoom,
        float panY
    )
        => -bar * chart.BeatsPerMeasure * zoom + panY
           - beat * zoom
           - (float)sixteenth / 4 * zoom;

    public static bool IsHoldNote(GodotNote note) 
        => note.EndBar != 0 || note.EndBeat != 0 || note.EndSixteenth != 0;

    public static bool NoteExistsAt(
        Chart chart,
        NoteTime time,
        NoteType type
    ) {
        return chart.Notes.Any(note =>
            note.Bar == time.Bar &&
            note.Beat == time.Beat &&
            Math.Abs(note.Sixteenth - time.Sixteenth) < 0.01 &&
            note.Type == type
        );
    }

    public static bool IsValidHoldNoteEnd(
        NoteTime start,
        NoteTime end,
        Chart chart
    ) {
        var startTotal = start.Bar * chart.BeatsPerMeasure * 4 + start.Beat * 4 + start.Sixteenth;
        var endTotal = end.Bar * chart.BeatsPerMeasure * 4 + end.Beat * 4 + end.Sixteenth;
        return endTotal > startTotal;
    }
}
