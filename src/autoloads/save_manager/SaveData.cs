using Godot;
using Godot.Collections;


namespace SYNK33.Saving;

interface ISaveInfo {
    public long GetChartPerformance(long chartUID);

    public void SetChartPerformance(long chartUID, long points);
}

public partial class SaveData : Resource, ISaveInfo
{
    /// <summary>
    /// Whether the player has completed the tutorial.
    /// </summary>
    [Export] public bool TutorialCompleted = false;
    /// <summary>
    /// Points highscore of charts by their resource UID.
    /// </summary>
    [Export] private Dictionary<long, long> ChartMap = [];

    public long GetChartPerformance(long chartUID)
    {
        long points = -1;
        ChartMap.TryGetValue(chartUID, out points);
        return points;
    }

    public void SetChartPerformance(long chartUID, long points)
    {
        ChartMap[chartUID] = points;
    }
}