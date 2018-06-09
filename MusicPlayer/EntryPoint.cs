using Musegician.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Musegician
{
    public class EntryPoint
    {
        [STAThread]
        static void Main()
        {
            using (FileManager.Instance = new FileManager())
            using (Player.MusicManager.Instance = new Player.MusicManager())
            using (KeyboardHook hook = new KeyboardHook(Player.MusicManager.GetHookKeys()))
            {
                Playlist.PlaylistManager.Instance.Initialize();

                hook.RegisteredKeyPressed += Player.MusicManager.Instance.RegisteredKeyPressed;

                App app = new App();
                app.InitializeComponent();
                app.Run();
            }
        }
    }
}
