using Godot;
using Godot.Collections;


namespace SYNK33.Saving;

interface ISaveInfo
{
    public long GetSongPerformance(string chartUID);

    public void SetSongPerformance(string chartUID, long points);
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
    [Export] private Dictionary<string, long> ChartMap = [];

    public long GetSongPerformance(string chartUID)
    {
        long points = -1;
        ChartMap.TryGetValue(chartUID, out points);
        return points;
    }

    public void SetSongPerformance(string chartUID, long points)
    {
        ChartMap[chartUID] = points;
    }
}