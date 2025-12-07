using System.Collections.Generic;
using Godot;
using SYNK33.chart;

namespace SYNK33.editor;

public static class EditorPlayback {
    public static void CheckAndPlayNoteHits(
        Chart chart,
        AudioStreamPlayer? audioStreamPlayer,
        AudioStreamPlayer? hitSoundPlayer,
        AudioStreamPlayer? holdSoundPlayer,
        HashSet<(long, long, double, NoteType)> playedNotes,
        HashSet<(long, long, double, NoteType)> activeHoldNotes) {
        
        if (audioStreamPlayer == null || hitSoundPlayer == null || holdSoundPlayer == null) return;
        if (!audioStreamPlayer.Playing) return;
        
        var songPosition = audioStreamPlayer.GetPlaybackPosition() +
            AudioServer.GetTimeSinceLastMix() - AudioServer.GetOutputLatency();
        var secondsPerBeat = 60.0f / chart.Bpm;
        
        foreach (var note in chart.Notes) {
            var noteId = (note.Bar, note.Beat, note.Sixteenth, note.Type);
            var noteTimeInSeconds = (note.Bar * chart.BeatsPerMeasure + note.Beat + note.Sixteenth / 4.0) * secondsPerBeat;
            var isHoldNote = EditorNoteHelpers.IsHoldNote(note);
            
            if (isHoldNote) {
                HandleHoldNotePlayback(
                    noteId,
                    noteTimeInSeconds,
                    note,
                    songPosition,
                    secondsPerBeat,
                    chart,
                    hitSoundPlayer,
                    holdSoundPlayer,
                    playedNotes,
                    activeHoldNotes
                );
            } else {
                HandleTapNotePlayback(
                    noteId,
                    noteTimeInSeconds,
                    songPosition,
                    hitSoundPlayer,
                    playedNotes
                );
            }
        }
    }

    private static void HandleHoldNotePlayback(
        (long, long, double, NoteType) noteId,
        double noteTimeInSeconds,
        GodotNote note,
        double songPosition,
        float secondsPerBeat,
        Chart chart,
        AudioStreamPlayer hitSoundPlayer,
        AudioStreamPlayer holdSoundPlayer,
        HashSet<(long, long, double, NoteType)> playedNotes,
        HashSet<(long, long, double, NoteType)> activeHoldNotes) {
        
        var endTimeInSeconds = (note.EndBar * chart.BeatsPerMeasure + note.EndBeat + note.EndSixteenth / 4.0) * secondsPerBeat;
        
        if (songPosition >= noteTimeInSeconds && songPosition <= endTimeInSeconds) {
            if (!activeHoldNotes.Contains(noteId)) {
                holdSoundPlayer.Play();
                activeHoldNotes.Add(noteId);
            }
        } else if (activeHoldNotes.Remove(noteId) && activeHoldNotes.Count == 0) {
            holdSoundPlayer.Stop();
        }
        
        var timeDiff = songPosition - noteTimeInSeconds;
        if (timeDiff is >= 0 and <= 0.1 && !playedNotes.Contains(noteId)) {
            hitSoundPlayer.Play();
            playedNotes.Add(noteId);
        }
    }

    private static void HandleTapNotePlayback(
        (long, long, double, NoteType) noteId,
        double noteTimeInSeconds,
        double songPosition,
        AudioStreamPlayer hitSoundPlayer,
        HashSet<(long, long, double, NoteType)> playedNotes) {
        
        var timeDiff = songPosition - noteTimeInSeconds;
        if (timeDiff is >= 0 and <= 0.1 && !playedNotes.Contains(noteId)) {
            hitSoundPlayer.Play();
            playedNotes.Add(noteId);
        }
    }

    public static void PlayPreview(
        AudioStreamPlayer? audioStreamPlayer,
        NoteTime selectedTime,
        Chart chart,
        HashSet<(long, long, double, NoteType)> playedNotes) {
        
        var seekTime = selectedTime.ToMilliseconds(chart.BeatsPerMeasure, 60.0f / chart.Bpm);
        GD.Print($"Playing preview from Bar:{selectedTime.Bar} Beat:{selectedTime.Beat} ({seekTime:F2}s)");
        
        playedNotes.Clear();
        audioStreamPlayer?.Play();
        audioStreamPlayer?.Seek(seekTime);
    }

    public static void StopPreview(
        AudioStreamPlayer? audioStreamPlayer,
        AudioStreamPlayer? holdSoundPlayer,
        HashSet<(long, long, double, NoteType)> playedNotes,
        HashSet<(long, long, double, NoteType)> activeHoldNotes) {
        
        GD.Print("Stopping preview");
        audioStreamPlayer?.Stop();
        holdSoundPlayer?.Stop();
        playedNotes.Clear();
        activeHoldNotes.Clear();
    }
}
