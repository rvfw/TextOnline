using System.Text;
using TextOnline.Models;

namespace TextOnline.Logic
{
    public class RoomService
    {
        public static string GetTextVersion(Room room, SavedText[] history, int version)
        {
            StringBuilder res = new();
            res.Append(room.Text);
            for (int i = history.Count() - 1; i >= version; i--)
            {
                if (history[i].TextEvent == "add")
                    res.Remove(history[i].Position, history[i].Text.Length);
                else if (history[i].TextEvent == "delete")
                    res.Insert(history[i].Position, history[i].Text);
            }
            return res.ToString();
        }
    }
}
