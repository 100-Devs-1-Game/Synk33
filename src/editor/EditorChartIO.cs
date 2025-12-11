using System;
using Godot;
using SYNK33.chart;

namespace SYNK33.editor;

public static class EditorChartIO {
    public static void SaveChart(
        EditorState state,
        Control parent,
        FileDialog.FileSelectedEventHandler onFileSelected
    ) {
        var fileDialog = CreateFileDialog(
            state,
            FileDialog.FileModeEnum.SaveFile,
            "Save Chart"
        );
        
        fileDialog.FileSelected += onFileSelected;
        ShowFileDialogAndCleanup(parent, fileDialog);
    }

    public static void LoadChart(
        EditorState state,
        Control parent,
        FileDialog.FileSelectedEventHandler onFileSelected
    ) {
        var fileDialog = CreateFileDialog(
            state,
            FileDialog.FileModeEnum.OpenFile,
            "Load Chart"
        );
        
        fileDialog.FileSelected += onFileSelected;
        ShowFileDialogAndCleanup(parent, fileDialog);
    }

    private static FileDialog CreateFileDialog(
        EditorState state,
        FileDialog.FileModeEnum fileMode,
        string title
    ) {
        var fileDialog = new FileDialog {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = fileMode,
            Filters = ["*.tres ; Godot Resource File"],
            Title = title,
            CurrentDir = string.IsNullOrEmpty(state.CurrentChartPath) 
                ? "res://assets/charts/" 
                : System.IO.Path.GetDirectoryName(state.CurrentChartPath)
        };
        
        if (!string.IsNullOrEmpty(state.CurrentChartPath)) {
            fileDialog.CurrentPath = state.CurrentChartPath;
        }

        return fileDialog;
    }

    private static void ShowFileDialogAndCleanup(Control parent, FileDialog fileDialog) {
        parent.AddChild(fileDialog);
        fileDialog.PopupCentered(new Vector2I(800, 600));
        
        fileDialog.Canceled += fileDialog.QueueFree;
        
        fileDialog.FileSelected += (_) => {
            fileDialog.CallDeferred(Node.MethodName.QueueFree);
        };
    }

    public static void OnSaveChartFileSelected(
        string path,
        EditorState state,
        Chart chart,
        Label statusLabel,
        SceneTree sceneTree
    ) {
        GD.Print($"Saving chart to: {path}");
        
        SortNotesByTime(chart);
        var error = ResourceSaver.Save(chart, path);
        
        if (error == Error.Ok) {
            state.CurrentChartPath = path;
            statusLabel.Text = $"Saved: {System.IO.Path.GetFileName(path)}";
            GD.Print($"Chart saved successfully");
            PersistChartPath(path);
        } else {
            statusLabel.Text = $"Save failed: {error}";
            GD.PrintErr($"Failed to save: {error}");
        }
        
        sceneTree.CreateTimer(3.0).Timeout += () => statusLabel.Text = "";
    }

    private static void SortNotesByTime(Chart chart) {
        var sortedNotes = new System.Collections.Generic.List<GodotNote>(chart.Notes);
        sortedNotes.Sort((a, b) => {
            if (a.Bar != b.Bar) return a.Bar.CompareTo(b.Bar);
            if (a.Beat != b.Beat) return a.Beat.CompareTo(b.Beat);
            return a.Sixteenth.CompareTo(b.Sixteenth);
        });
        chart.Notes = new Godot.Collections.Array<GodotNote>(sortedNotes);
    }

    private static void PersistChartPath(string path) {
        try {
            using var fa = FileAccess.Open("user://last_chart_path.txt", FileAccess.ModeFlags.Write);
            fa.StoreString(path);
            fa.Close();
        } catch (Exception e) {
            GD.PrintErr($"Failed to persist last chart path: {e.Message}");
        }
    }


    public static void OnLoadChartFileSelected(
        string path, 
        EditorState state, 
        ref Chart chart, 
        Label statusLabel, 
        SceneTree sceneTree, 
        Action updateInfoDisplay, 
        Action queueRedraw,
        AudioStreamPlayer? audioPlayer
    ) {
        GD.Print($"Loading chart from: {path}");
        var loadedChart = ResourceLoader.Load<Chart>(path);
        
        if (loadedChart != null) {
            chart = loadedChart;
            state.CurrentChartPath = path;
            PersistChartPath(path);
            SortNotesByTime(chart);
            LoadAudioFromChart(chart, audioPlayer);
            
            statusLabel.Text = $"Loaded: {System.IO.Path.GetFileName(path)} ({chart.Notes.Count} notes)";
            GD.Print($"Chart loaded: {chart.Notes.Count} notes");
            updateInfoDisplay();
            queueRedraw();
        } else {
            statusLabel.Text = "Load failed: Invalid file";
            GD.PrintErr($"Failed to load from {path}");
        }
        
        sceneTree.CreateTimer(3.0).Timeout += () => statusLabel.Text = "";
    }

    private static void LoadAudioFromChart(Chart chart, AudioStreamPlayer? audioPlayer) {
        if (audioPlayer == null) return;
        
        if (chart.Song?.Audio != null) {
            audioPlayer.Stream = chart.Song.Audio;
            GD.Print($"Loaded audio from song: {chart.Song.Name}");
        } else {
            audioPlayer.Stream = null;
            GD.PrintErr("Chart loaded but no audio available in Song");
        }
    }
}
