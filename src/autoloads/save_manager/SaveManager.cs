using Godot;
using Godot.Collections;


namespace SYNK33.Saving;


public partial class SaveManager : Node, ISaveInfo
{
    

    private const string SavePath = "user://save.tres"; // TODO: in Release this shouldn't be .tres

    private readonly SaveData save;
    
    public SaveManager()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            save = new SaveData();
            return;
        }
        save = ResourceLoader.Load<SaveData>(SavePath);
    }
    // TODO: When saving actually matters, uncomment this
    /*public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            ResourceSaver.Save(save, SavePath);
        }
    }*/

    public long GetChartPerformance(long chartUID)
    {
        return save.GetChartPerformance(chartUID);
    }

    public void SetChartPerformance(long chartUID, long points)
    {
        save.SetChartPerformance(chartUID, points);
    }
}
