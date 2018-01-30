using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Musegician.Spatializer
{
    /// <summary>
    /// Interaction logic for SpeakerVisualization.xaml
    /// </summary>
    public partial class SpeakerVisualization : UserControl
    {
        #region DependencyProperties
        #region ShowLL

        public static readonly DependencyProperty ShowLLProperty =
            DependencyProperty.Register("ShowLL", typeof(bool),
              typeof(SpeakerVisualization));

        public string ShowLL
        {
            get { return (string)GetValue(ShowLLProperty); }
            set { SetValue(ShowLLProperty, value); }
        }
        #endregion ShowLL
        #region ShowLR

        public static readonly DependencyProperty ShowLRProperty =
            DependencyProperty.Register("ShowLR", typeof(bool),
              typeof(SpeakerVisualization));

        public string ShowLR
        {
            get { return (string)GetValue(ShowLRProperty); }
            set { SetValue(ShowLRProperty, value); }
        }

        #endregion ShowLR
        #region ShowRL

        public static readonly DependencyProperty ShowRLProperty =
            DependencyProperty.Register("ShowRL", typeof(bool),
              typeof(SpeakerVisualization));

        public string ShowRL
        {
            get { return (string)GetValue(ShowRLProperty); }
            set { SetValue(ShowRLProperty, value); }
        }
        #endregion ShowRL
        #region ShowRR

        public static readonly DependencyProperty ShowRRProperty =
            DependencyProperty.Register("ShowRR", typeof(bool),
              typeof(SpeakerVisualization));

        public string ShowRR
        {
            get { return (string)GetValue(ShowRRProperty); }
            set { SetValue(ShowRRProperty, value); }
        }

        #endregion ShowRR
        #endregion DependencyProperties
        #region Constructor

        public SpeakerVisualization()
        {
            InitializeComponent();
        }

        #endregion Constructor
    }
}
