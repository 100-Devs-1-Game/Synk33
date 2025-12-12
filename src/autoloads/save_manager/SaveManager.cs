using System;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Collections;
using SYNK33.chart;


namespace SYNK33.Saving;


public partial class SaveManager : Node, ISaveInfo
{
    private const string SavePath = "user://save.sav";
    private const string ChartMapSavePath = "user://chart_map_save.dat";

    private readonly SaveData save;
    
    public SaveManager() {
        save = new SaveData();
        if (!FileAccess.FileExists(SavePath)) {
            Save();
            return;
        }
        Load();
    }
    // TODO: When saving actually matters, uncomment this
    /*public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            ResourceSaver.Save(save, SavePath);
        }
    }*/
    public bool HasChartPerformance(long chartHash) {
        return save.HasChartPerformance(chartHash);
    }
    
    public ChartPerformance? GetChartPerformance(long chartHash) {
        return save.GetChartPerformance(chartHash);
    }

    public long GetChartHighscore(long chartHash) {
        ChartPerformance? performance = save.GetChartPerformance(chartHash);
        if (!performance.HasValue) {
            return -1;
        }
        return performance.Value.Highscore;
    }

    public void SetChartPerformance(long chartHash, ChartPerformance chartPerformance) {
        save.SetChartPerformance(chartHash, chartPerformance);
    }

    public void Save() {
        save.Save(SavePath);
        FileAccess file = FileAccess.Open(ChartMapSavePath, FileAccess.ModeFlags.Write);
        save.SerializeChartMap(file);
        file.Close();
    }

    public void Load() {
        save.Load(SavePath);
        FileAccess file = FileAccess.Open(ChartMapSavePath, FileAccess.ModeFlags.Read);
        save.DeserializeChartMap(file);
        file.Close();
    }

    public void PrintoutChartMap() {
        FileAccess file = FileAccess.Open(ChartMapSavePath, FileAccess.ModeFlags.Read);
        save.PrintoutChartMap(file);
        file.Close();
    }
}
