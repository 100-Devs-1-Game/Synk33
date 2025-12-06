using System;
using Godot;
using SYNK33.chart;

namespace SYNK33.editor;

public static class EditorView {
    public static void PanView(
        EditorState state,
        InputEventMouseMotion mouseMotionEvent,
        Action queueRedraw
    ) {
        var deltaY = mouseMotionEvent.Position.Y - state.DragStartMouse.Y;
        state.PanY = state.DragStartPan + deltaY;
        queueRedraw();
    }

    public static void TogglePanView(
        EditorState state, 
        InputEventMouseButton mouseEvent
    ) {
        if (mouseEvent.Pressed) {
            state.IsDragging = true;
            state.DragStartMouse = mouseEvent.Position;
            state.DragStartPan = state.PanY;
        } else {
            state.IsDragging = false;
        }
    }

    public static void ZoomAtMouse(
        EditorState state,
        float newZoom,
        Vector2 mousePosition,
        Action queueRedraw
    ) {
        var oldZoom = state.Zoom;
        state.Zoom = Math.Clamp(newZoom, EditorConstants.MinZoom, EditorConstants.MaxZoom);
        var previousPos = (mousePosition.Y - state.PanY) / oldZoom;
        state.PanY = mousePosition.Y - previousPos * state.Zoom;
        GD.Print($"Zoom: {oldZoom:F1} \u2192 {state.Zoom:F1}");
        queueRedraw();
    }
}
