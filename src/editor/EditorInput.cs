using Godot;

namespace SYNK33.editor;

public static class EditorInput {
    public static void HandleMouseButton(InputEventMouseButton mouseEvent, Editor editor) {
        switch (mouseEvent.ButtonIndex) {
            case MouseButton.Left when mouseEvent.Pressed:
                editor.AddNote();
                break;
            case MouseButton.Left when !mouseEvent.Pressed:
                break;
            case MouseButton.Right when mouseEvent.Pressed:
                editor.RemoveNote();
                break;
            case MouseButton.WheelUp:
                EditorView.ZoomAtMouse(editor.State, editor.State.Zoom * 1.1f, mouseEvent.Position, editor.QueueRedraw);
                break;
            case MouseButton.WheelDown:
                EditorView.ZoomAtMouse(editor.State, editor.State.Zoom / 1.1f, mouseEvent.Position, editor.QueueRedraw);
                break;
            case MouseButton.Middle:
                EditorView.TogglePanView(editor.State, mouseEvent);
                break;
        }
    }

    public static void HandleKeyInput(InputEventKey keyEvent, Editor editor) {
        if (keyEvent.IsActionPressed("editor_play") || keyEvent is { Keycode: Key.Space, Pressed: true }) {
            editor.TogglePlayback();
            return;
        }
        
        if (!keyEvent.Pressed || keyEvent.Echo) return;
        
        HandleEditorCommands(keyEvent, editor);
        HandleSnappingControls(keyEvent, editor);
    }

    private static void HandleEditorCommands(InputEventKey keyEvent, Editor editor) {
        switch (keyEvent) {
            case { CtrlPressed: true, Keycode: Key.S }:
                editor.SaveChart();
                break;
            case { CtrlPressed: true, Keycode: Key.O }:
                editor.LoadChart();
                break;
            default: {
                switch (keyEvent.Keycode) {
                    case Key.T:
                        editor.ToggleNoteMode();
                        break;
                    case Key.Y:
                        editor.ToggleTripletMode();
                        break;
                }
                break;
            }
        }
    }

    private static void HandleSnappingControls(InputEventKey keyEvent, Editor editor) {
        var oldSnapping = editor.State.Snapping;
        editor.State.Snapping = keyEvent.Keycode switch {
            Key.Key1 => 1,
            Key.Key2 => 2,
            Key.Key3 => 4,
            Key.Key4 => 8,
            Key.Key5 => 16,
            Key.Key6 => 32,
            Key.Key7 => 64,
            Key.Equal or Key.Plus => System.Math.Min(editor.State.Snapping * 2, 64),
            Key.Minus => System.Math.Max(editor.State.Snapping / 2, 1),
            _ => editor.State.Snapping
        };
        
        if (oldSnapping != editor.State.Snapping) {
            editor.UpdateGridButtons();
        }
    }
}
