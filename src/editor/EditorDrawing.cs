using System;
using Godot;
using SYNK33.chart;

namespace SYNK33.editor;

public static class EditorDrawing {
    public static void DrawLaneBoundaries(CanvasItem canvas, float startX) {
        for (var i = 0; i <= EditorConstants.MaxLanes; i++) {
            canvas.DrawLine(
                new Vector2(startX + i * EditorConstants.LaneWidth, 0), 
                new Vector2(startX + i * EditorConstants.LaneWidth, canvas.GetViewportRect().Size.Y), 
                Colors.White
            );
        }
    }

    private static void DrawBarLines(CanvasItem canvas, int barIndex, Chart chart, float zoom, float panY) {
        var yPosition = -barIndex * zoom * chart.BeatsPerMeasure + panY;
        canvas.DrawLine(new Vector2(0, yPosition), new Vector2(canvas.GetViewportRect().Size.X, yPosition), Colors.White);
    }

    private static void DrawBeatLines(
        CanvasItem canvas, 
        int barIndex, 
        Chart chart, 
        int effectiveSnapping, 
        float zoom,
        float panY
    ) {
        var totalSixteenthsPerBar = chart.BeatsPerMeasure * 4.0;
        var snapGridSize = totalSixteenthsPerBar / effectiveSnapping;

        for (var j = 0; j < effectiveSnapping; j++) {
            if (j == 0) continue;

            var sixteenthPosition = j * snapGridSize;
            var beatPosition = sixteenthPosition / 4.0;

            var isBeatLine = Math.Abs(sixteenthPosition % 4.0) < 0.01;
            var color = isBeatLine
                ? new Color(0.6f, 0.6f, 0.6f)
                : new Color(0.4f, 0.4f, 0.4f);

            var yPosition = (float)(-barIndex * zoom * chart.BeatsPerMeasure + -beatPosition * zoom + panY);
            canvas.DrawLine(new Vector2(0, yPosition), new Vector2(canvas.GetViewportRect().Size.X, yPosition), color);
        }
    }

    public static void DrawGridLines(
        CanvasItem canvas, 
        AudioStreamPlayer? audioStreamPlayer, 
        Chart chart, 
        int effectiveSnapping, 
        float zoom, 
        float panY
    ) {
        var songLength = audioStreamPlayer?.Stream.GetLength() / 60 * chart.Bpm / chart.BeatsPerMeasure;
        for (var i = 0; i < songLength; i++) {
            DrawBeatLines(canvas, i, chart, effectiveSnapping, zoom, panY);
            DrawBarLines(canvas, i, chart, zoom, panY);
        }
    }

    private static void DrawTapNote(CanvasItem canvas, float laneLeft, float laneWidthInner, float noteY, Color baseColor) {
        var bgColor = baseColor.Darkened(0.2f);
        canvas.DrawRect(new Rect2(laneLeft, noteY - EditorConstants.TapNoteHeight / 2, laneWidthInner, EditorConstants.TapNoteHeight), bgColor);
        canvas.DrawRect(new Rect2(laneLeft + 2, noteY - EditorConstants.TapNoteHeight / 2 + 2, laneWidthInner - 4, EditorConstants.TapNoteHeight - 4), baseColor);
    }

    private static void DrawHoldNote(
        CanvasItem canvas,
        float laneLeft,
        float laneWidthInner,
        float startY,
        float endY,
        Color baseColor,
        float hue
    ) {
        var topY = Math.Min(startY, endY);
        var bottomY = Math.Max(startY, endY);
        var bodyHeight = Math.Max(6f, bottomY - topY);

        var bodyColor = Color.FromHsv(hue, 0.8f, 1f, 0.6f);
        canvas.DrawRect(new Rect2(laneLeft, topY, laneWidthInner, bodyHeight), bodyColor);

        DrawHoldNoteHead(canvas, laneLeft, laneWidthInner, startY, baseColor, EditorConstants.HoldNoteStartHeadHeight, false);
        DrawHoldNoteHead(canvas, laneLeft, laneWidthInner, endY, baseColor, EditorConstants.HoldNoteEndHeadHeight, true);
    }

    private static void DrawHoldNoteHead(
        CanvasItem canvas,
        float laneLeft,
        float laneWidthInner,
        float yPosition,
        Color baseColor,
        float height,
        bool isEnd
    ) {
        var outerColor = baseColor.Darkened(isEnd ? 0.35f : 0.25f);
        var innerColor = isEnd ? baseColor.Lightened(0.1f) : baseColor;
        
        canvas.DrawRect(new Rect2(laneLeft, yPosition - height / 2, laneWidthInner, height), outerColor);
        canvas.DrawRect(new Rect2(laneLeft + 2, yPosition - height / 2 + 2, laneWidthInner - 4, height - 4), innerColor);
    }

    public static void DrawNotes(CanvasItem canvas, Chart chart, EditorState state, AudioStreamPlayer? audioStreamPlayer) {
        const int totalLaneWidth = EditorConstants.MaxLanes * EditorConstants.LaneWidth;
        var startX = (canvas.GetViewportRect().Size.X - totalLaneWidth) / 2;
        
        foreach (var godotNote in chart.Notes) {
            var laneIndex = EditorNoteHelpers.GetLaneIndex(godotNote.Type);
            var laneLeft = startX + laneIndex * EditorConstants.LaneWidth + EditorConstants.LanePadding;
            var laneWidthInner = EditorConstants.LaneWidth - 2 * EditorConstants.LanePadding;
            
            var noteY = EditorNoteHelpers.CalculateNoteYPosition(godotNote.Bar, godotNote.Beat, godotNote.Sixteenth, chart, state.Zoom, state.PanY);
            var baseColor = EditorNoteHelpers.GetLaneColor(godotNote.Type);
            
            if (EditorNoteHelpers.IsHoldNote(godotNote)) {
                var endY = EditorNoteHelpers.CalculateNoteYPosition(godotNote.EndBar, godotNote.EndBeat, godotNote.EndSixteenth, chart, state.Zoom, state.PanY);
                var hue = EditorNoteHelpers.GetHueForLane(godotNote.Type);
                DrawHoldNote(canvas, laneLeft, laneWidthInner, noteY, endY, baseColor, hue);
            } else {
                DrawTapNote(canvas, laneLeft, laneWidthInner, noteY, baseColor);
            }
        }
    }

    public static void DrawPlayhead(CanvasItem canvas, AudioStreamPlayer? audioPlayer, float zoom, float panY, float bpm) {
        if (audioPlayer is not { Playing: true }) return;
        
        var songPosition = audioPlayer.GetPlaybackPosition() +
            AudioServer.GetTimeSinceLastMix() - AudioServer.GetOutputLatency();
        var playPosition = -(float)songPosition / 60 * bpm * zoom + panY;
        canvas.DrawLine(new Vector2(0, playPosition), new Vector2(canvas.GetViewportRect().Size.X, playPosition), Colors.Red);
    }

    private static void DrawNormalSelector(CanvasItem canvas, float selectorX, float selectorY, NoteMode noteMode) {
        var color = noteMode == NoteMode.Tap 
            ? Color.FromHsv(120, 1, 1, 0.5f)
            : Color.FromHsv(60, 1, 1, 0.5f);
        canvas.DrawCircle(new Vector2(selectorX, selectorY), 15, color);
    }

    private static void DrawHoldNotePreview(
        CanvasItem canvas,
        float selectorX,
        float selectorY,
        float startY,
        float startXLane,
        int selectedLane,
        NoteType? holdNoteLane
    ) {
        canvas.DrawCircle(new Vector2(startXLane, startY), 15, Color.FromHsv(60, 1, 1, 0.7f));
        
        if (selectedLane == (int)holdNoteLane!) {
            canvas.DrawLine(new Vector2(startXLane, startY), new Vector2(selectorX, selectorY), 
                     Color.FromHsv(60, 1, 1, 0.5f), 3);
            canvas.DrawCircle(new Vector2(selectorX, selectorY), 15, Color.FromHsv(60, 1, 1, 0.5f));
        }
    }

    public static void DrawSelector(CanvasItem canvas, Chart chart, EditorState state) {
        const int totalLaneWidth = EditorConstants.MaxLanes * EditorConstants.LaneWidth;
        var startX = (canvas.GetViewportRect().Size.X - totalLaneWidth) / 2;
        var selectorY = EditorNoteHelpers.CalculateNoteYPosition(state.SelectedTime.Bar, state.SelectedTime.Beat, state.SelectedTime.Sixteenth, chart, state.Zoom, state.PanY);
        var selectorX = startX + state.SelectedLane * EditorConstants.LaneWidth + EditorConstants.LaneWidth / 2f;
        
        if (state is { NoteMode: NoteMode.Hold, HoldNoteStart: not null, HoldNoteLane: not null }) {
            var startY = EditorNoteHelpers.CalculateNoteYPosition(state.HoldNoteStart.Bar, state.HoldNoteStart.Beat, state.HoldNoteStart.Sixteenth, chart, state.Zoom, state.PanY);
            var startXLane = startX + (int)state.HoldNoteLane! * EditorConstants.LaneWidth + EditorConstants.LaneWidth / 2f;
            DrawHoldNotePreview(canvas, selectorX, selectorY, startY, startXLane, state.SelectedLane, state.HoldNoteLane);
        } else {
            DrawNormalSelector(canvas, selectorX, selectorY, state.NoteMode);
        }
    }

    public static void DrawCursorLine(CanvasItem canvas, EditorState state) {
        var mouseY = state.MousePosition.Y;
        var viewportWidth = canvas.GetViewportRect().Size.X;
        
        canvas.DrawLine(
            new Vector2(0, mouseY),
            new Vector2(viewportWidth, mouseY),
            new Color(1f, 1f, 1f, 0.3f),
            1f
        );
    }
}
