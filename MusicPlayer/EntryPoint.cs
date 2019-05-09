using Musegician.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CSCore;
using CSCore.Codecs;
using Musegician.Sources;

namespace Musegician
{
    public class EntryPoint
    {
        [STAThread]
        static void Main()
      {
            //Register our custom ogg reader
            CodecFactory.Instance.Register("ogg-vorbis", new CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));

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
