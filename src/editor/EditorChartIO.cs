using System;
using Godot;
using SYNK33.chart;

namespace SYNK33.editor;

public static class EditorChartIO {
    public static void SaveChart(EditorState state, Chart chart, Label statusLabel, Control parent, FileDialog.FileSelectedEventHandler onFileSelected) {
        var fileDialog = new FileDialog {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Filters = ["*.tres ; Godot Resource File"],
            Title = "Save Chart",
            CurrentDir = string.IsNullOrEmpty(state.CurrentChartPath) 
                ? "res://assets/charts/" 
                : System.IO.Path.GetDirectoryName(state.CurrentChartPath)
        };
        
        if (!string.IsNullOrEmpty(state.CurrentChartPath)) {
            fileDialog.CurrentPath = state.CurrentChartPath;
        }
        
        fileDialog.FileSelected += onFileSelected;
        parent.AddChild(fileDialog);
        fileDialog.PopupCentered(new Vector2I(800, 600));
    }

    public static void OnSaveChartFileSelected(string path, EditorState state, Chart chart, Label statusLabel, SceneTree sceneTree) {
        GD.Print($"Saving chart to: {path}");
        var error = ResourceSaver.Save(chart, path);
        
        if (error == Error.Ok) {
            state.CurrentChartPath = path;
            statusLabel.Text = $"Saved: {System.IO.Path.GetFileName(path)}";
            GD.Print($"Chart saved successfully");
        } else {
            statusLabel.Text = $"Save failed: {error}";
            GD.PrintErr($"Failed to save: {error}");
        }
        
        sceneTree.CreateTimer(3.0).Timeout += () => statusLabel.Text = "";
    }

    public static void LoadChart(EditorState state, Control parent, FileDialog.FileSelectedEventHandler onFileSelected) {
        var fileDialog = new FileDialog {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Filters = ["*.tres ; Godot Resource File"],
            Title = "Load Chart",
            CurrentDir = string.IsNullOrEmpty(state.CurrentChartPath) 
                ? "res://assets/charts/" 
                : System.IO.Path.GetDirectoryName(state.CurrentChartPath)
        };
        
        if (!string.IsNullOrEmpty(state.CurrentChartPath)) {
            fileDialog.CurrentPath = state.CurrentChartPath;
        }
        
        fileDialog.FileSelected += onFileSelected;
        parent.AddChild(fileDialog);
        fileDialog.PopupCentered(new Vector2I(800, 600));
    }

    public static void OnLoadChartFileSelected(string path, EditorState state, ref Chart chart, Label statusLabel, 
                                                SceneTree sceneTree, Action updateInfoDisplay, Action queueRedraw) {
        GD.Print($"Loading chart from: {path}");
        var loadedChart = ResourceLoader.Load<Chart>(path);
        
        if (loadedChart != null) {
            chart = loadedChart;
            state.CurrentChartPath = path;
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
}

