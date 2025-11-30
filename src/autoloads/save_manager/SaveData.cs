using Godot;
using Godot.Collections;


namespace SYNK33.Saving;

interface ISaveInfo
{
    public long GetSongPerformance(StringName song);

    public void SetSongPerformance(StringName song, long points);
}

public partial class SaveData : Resource, ISaveInfo
{
    /// <summary>
    /// Whether the player has completed the tutorial.
    /// </summary>
    [Export] public bool TutorialCompleted = false;
    /// <summary>
    /// Points highscore of songs
    /// </summary>
    [Export] private Dictionary<StringName, long> SongMap = [];

    public long GetSongPerformance(StringName song)
    {
        long points = -1;
        SongMap.TryGetValue(song, out points);
        return points;
    }

    public void SetSongPerformance(StringName song, long points)
    {
        SongMap[song] = points;
    }
}