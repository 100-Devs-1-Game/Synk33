using Godot;
using Godot.Collections;


namespace SYNK33.Saving;


public partial class SaveManager : Node, ISaveInfo
{
    

    private const string SavePath = "user://save.tres"; // TODO: We

    private SaveData save;
    
    public SaveManager()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            save = new SaveData();
            return;
        }
        save = ResourceLoader.Load<SaveData>(SavePath);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            ResourceSaver.Save(save, SavePath);
        }
    }

    public long GetSongPerformance(StringName song)
    {
        return save.GetSongPerformance(song);
    }

    public void SetSongPerformance(StringName song, long points)
    {
        save.SetSongPerformance(song, points);
    }
}
