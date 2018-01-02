using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MusicPlayer
{
    public class EntryPoint
    {
        [STAThread]
        static void Main()
        {
            using (Player.MusicManager.Instance = new Player.MusicManager())
            using (KeyboardHook hook = new KeyboardHook(Player.MusicManager.GetHookKeys()))
            {
                hook.RegisteredKeyPressed += Player.MusicManager.Instance.RegisteredKeyPressed;

                App app = new App();
                app.InitializeComponent();
                app.Run();
            }
        }
    }
}
