using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Musegician
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            SessionEnding += App_SessionEnding;
        }

        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            Player.MusicManager.Instance.CleanUp();
            Shutdown();
        }
    }
}
