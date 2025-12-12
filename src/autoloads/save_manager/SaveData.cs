using System;
using Godot;
using Godot.Collections;


namespace SYNK33.Saving;

public struct ChartPerformance {
    public uint Highscore;
    public uint PerfectHits;
    public uint TotalHits;
    public uint GhostHits;
    public uint ChartTotalNotes;
    public uint ChartTotalGhostNotes;
    public uint MaxCombo;

    public readonly double GetGradeRatio() {
        return TotalHits / (double)ChartTotalNotes;
    }
    public readonly uint GetMisses() {
        return ChartTotalNotes - TotalHits;
    }
}

interface ISaveInfo {
    public bool HasChartPerformance(long chartHash);
    public ChartPerformance? GetChartPerformance(long chartHash);
    public void SetChartPerformance(long chartHash, ChartPerformance chartPerformance);
}

public partial class SaveData : Resource, ISaveInfo{
    [ExportGroup("StoryFlags")]
    /// <summary>
    /// Whether the player has completed the tutorial.
    /// </summary>
    [Export] public bool TutorialCompleted = false;
    /// <summary>
    /// Points performance of charts by their resource UID.
    /// </summary>
    private System.Collections.Generic.Dictionary<long, ChartPerformance> ChartMap = [];

    public bool HasChartPerformance(long chartHash) {
        return ChartMap.ContainsKey(chartHash);
    }
    public ChartPerformance? GetChartPerformance(long chartHash) {
        if (ChartMap.ContainsKey(chartHash)) {
            return null;
        }
        return ChartMap[chartHash];
    }

    public void SetChartPerformance(long chartHash, ChartPerformance chartPerformance) {
        ChartMap[chartHash] = chartPerformance;
    }

    public void Save(string path) {
        ConfigFile config = new ConfigFile();
        config.SetValue("story_flags", "tutorial_completed", TutorialCompleted);
        config.Save(path);
    }

    public void Load(string path) {
        ConfigFile config = new ConfigFile();
        config.Load(path);
        TutorialCompleted = (bool)config.GetValue("story_flags", "tutorial_completed", false);
    }

    public void SerializeChartMap(FileAccess file) {
        file.Store32((uint)ChartMap.Count);
        foreach (System.Collections.Generic.KeyValuePair<long, ChartPerformance> kvPerformance in ChartMap) {
            file.Store64((ulong)kvPerformance.Key);
            ChartPerformance chartPerformance = kvPerformance.Value;
            file.Store32(chartPerformance.Highscore);
            file.Store32(chartPerformance.PerfectHits);
            file.Store32(chartPerformance.TotalHits);
            file.Store32(chartPerformance.GhostHits);
            file.Store32(chartPerformance.ChartTotalNotes);
            file.Store32(chartPerformance.ChartTotalGhostNotes);
            file.Store32(chartPerformance.MaxCombo);
        }
    }

    public void DeserializeChartMap(FileAccess file) {
        uint count = file.Get32();
        for(uint i = 0; i < count; i++) {
            long chartHash = (long)file.Get64();
            ChartPerformance chartPerformance = new ChartPerformance {
                Highscore = file.Get32(),
                PerfectHits = file.Get32(),
                TotalHits = file.Get32(),
                GhostHits = file.Get32(),
                ChartTotalNotes = file.Get32(),
                ChartTotalGhostNotes = file.Get32(),
                MaxCombo = file.Get32(),
            };
            SetChartPerformance(chartHash, chartPerformance);
        }
    }

    public void PrintoutChartMap(FileAccess file) {
        uint count = file.Get32();
        GD.Print($"Count: {count}");
        for(uint i = 0; i < count; i++){
            GD.Print($"({i})\n\tChart UID:{file.Get64()}");
            GD.Print($"\tHighscore:{file.Get32()}");
            GD.Print($"\tPerfect Hits:{file.Get32()}");
            GD.Print($"\tTotal Hits:{file.Get32()}");
            GD.Print($"\tGhost Hits:{file.Get32()}");
            GD.Print($"\tChart Total Notes:{file.Get32()}");
            GD.Print($"\tChart Total Ghost Notes:{file.Get32()}");
            GD.Print($"\tMax Combo:{file.Get32()}");
        }
    }
}