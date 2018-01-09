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
            Exit += App_Exit;

            //System.Windows.Input.Keyboard.AddKeyDownHandler()
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            //Player.MusicManager.Instance.CleanUp();
        }
    }
}
