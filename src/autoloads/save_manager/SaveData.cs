using System;
using Godot;
using Godot.Collections;


namespace SYNK33.Saving;

public struct ChartPerformance {
    public uint Highscore;
    public uint PerfectHits;
    public uint TotalHits;
    public uint ChartTotalNotes;

    public readonly double GetGradeRatio() {
        return TotalHits / (double)ChartTotalNotes;
    }
    public readonly uint GetMisses() {
        return ChartTotalNotes - TotalHits;
    }
}

interface ISaveInfo {
    public bool HasChartPerformance(long chartUID);
    public ChartPerformance? GetChartPerformance(long chartUID);
    public void SetChartPerformance(long chartUID, ChartPerformance chartPerformance);
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

    public bool HasChartPerformance(long chartUID) {
        return ChartMap.ContainsKey(chartUID);
    }
    public ChartPerformance? GetChartPerformance(long chartUID) {
        if (ChartMap.ContainsKey(chartUID)) {
            return null;
        }
        return ChartMap[chartUID];
    }

    public void SetChartPerformance(long chartUID, ChartPerformance chartPerformance) {
        ChartMap[chartUID] = chartPerformance;
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
            file.Store32(chartPerformance.ChartTotalNotes);
        }
    }

    public void DeserializeChartMap(FileAccess file) {
        uint count = file.Get32();
        for(uint i = 0; i < count; i++) {
            long chartUID = (long)file.Get64();
            ChartPerformance chartPerformance = new ChartPerformance {
                Highscore = file.Get32(),
                PerfectHits = file.Get32(),
                TotalHits = file.Get32(),
                ChartTotalNotes = file.Get32()
            };
            SetChartPerformance(chartUID, chartPerformance);
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
            GD.Print($"\tChart Total Notes:{file.Get32()}");
        }
    }
}