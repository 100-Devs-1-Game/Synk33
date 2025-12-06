using System;
using System.Collections.Generic;
using Godot;
using SYNK33.chart;

namespace SYNK33.editor;

public static class EditorNotePlacement {
    public static void AddNote(
        Chart chart,
        EditorState state,
        HashSet<(int, int, double, NoteType)> playedNotes,
        Action updateInfoDisplay,
        Action queueRedraw
    ) {
        var noteType = EditorNoteHelpers.GetNoteTypeFromLane(state.SelectedLane);
        
        if (state.NoteMode == NoteMode.Tap) {
            AddTapNote(chart, state, noteType, playedNotes, updateInfoDisplay, queueRedraw);
        } else {
            AddHoldNote(chart, state, noteType, playedNotes, updateInfoDisplay, queueRedraw);
        }
    }

    private static void AddTapNote(
        Chart chart,
        EditorState state,
        NoteType noteType,
        HashSet<(int, int, double, NoteType)> playedNotes,
        Action updateInfoDisplay,
        Action queueRedraw
    ) {
        if (EditorNoteHelpers.NoteExistsAt(chart, state.SelectedTime, noteType)) {
            GD.Print($"Note already exists at {state.SelectedTime}");
            return;
        }
        
        GD.Print($"Adding tap note at {state.SelectedTime} Lane:{noteType}");
        var godotNote = new GodotNote(new Note.Tap(state.SelectedTime, noteType));
        chart.Notes.Add(godotNote);
        
        if (state.IsPlaying) {
            playedNotes.Remove((state.SelectedTime.Bar, state.SelectedTime.Beat, state.SelectedTime.Sixteenth, noteType));
        }
        
        updateInfoDisplay();
        queueRedraw();
    }

    private static void AddHoldNote(
        Chart chart,
        EditorState state,
        NoteType noteType,
        HashSet<(int, int, double, NoteType)> playedNotes,
        Action updateInfoDisplay,
        Action queueRedraw
    ) {
        if (state.HoldNoteStart == null) {
            StartHoldNotePlacement(state, noteType);
        } else {
            CompleteHoldNotePlacement(chart, state, noteType, playedNotes, updateInfoDisplay, queueRedraw);
        }
    }

    private static void StartHoldNotePlacement(EditorState state, NoteType noteType) {
        state.HoldNoteStart = state.SelectedTime;
        state.HoldNoteLane = noteType;
        GD.Print($"Hold note start: {state.SelectedTime} Lane:{noteType}");
    }

    private static void CompleteHoldNotePlacement(
        Chart chart,
        EditorState state,
        NoteType noteType,
        HashSet<(int, int, double, NoteType)> playedNotes,
        Action updateInfoDisplay,
        Action queueRedraw
    ) {
        if (noteType != state.HoldNoteLane) {
            GD.Print("Hold note must be on same lane");
            CancelHoldNotePlacement(state);
            return;
        }
        
        if (!EditorNoteHelpers.IsValidHoldNoteEnd(state.HoldNoteStart!, state.SelectedTime, chart)) {
            GD.Print("Hold note end must be after start");
            CancelHoldNotePlacement(state);
            return;
        }
        
        GD.Print($"Adding hold note from {state.HoldNoteStart} to {state.SelectedTime} Lane:{noteType}");
        var godotNote = new GodotNote(new Note.Hold(state.HoldNoteStart!, state.SelectedTime, noteType));
        chart.Notes.Add(godotNote);
        
        if (state.IsPlaying) {
            var noteId = (state.HoldNoteStart!.Bar, state.HoldNoteStart.Beat, state.HoldNoteStart.Sixteenth, noteType);
            playedNotes.Remove(noteId);
        }
        
        CancelHoldNotePlacement(state);
        updateInfoDisplay();
        queueRedraw();
    }

    private static void CancelHoldNotePlacement(EditorState state) {
        state.HoldNoteStart = null;
        state.HoldNoteLane = null;
    }

    public static void RemoveNote(
        Chart chart,
        EditorState state,
        Action updateInfoDisplay,
        Action queueRedraw
    ) {
        var noteType = EditorNoteHelpers.GetNoteTypeFromLane(state.SelectedLane);
        GD.Print($"Removing note at {state.SelectedTime} Lane:{noteType}");
        
        for (var i = chart.Notes.Count - 1; i >= 0; i--) {
            var note = chart.Notes[i];
            if (note.Bar == state.SelectedTime.Bar && 
                note.Beat == state.SelectedTime.Beat && 
                Math.Abs(note.Sixteenth - state.SelectedTime.Sixteenth) < 0.01 &&
                note.Type == noteType) {
                GD.Print($"Note removed at index {i}");
                chart.Notes.RemoveAt(i);
                updateInfoDisplay();
                queueRedraw();
                break;
            }
        }
    }
}
