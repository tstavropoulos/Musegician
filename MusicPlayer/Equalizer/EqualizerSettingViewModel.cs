using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Equalizer
{
    public class EqualizerSettingViewModel
    {
        #region Data

        EqualizerSettingDTO data;

        #endregion Data
        #region Constructor

        public EqualizerSettingViewModel(EqualizerSettingDTO data)
        {
            this.data = data;

            if (data.name == "Custom")
            {
                Hidden = true;
            }
        }

        #endregion Constructor
        #region Properties

        public string Name { get { return data.name; } }
        public float[] Gain { get { return data.gain; } }
        public bool Hidden { get; set; }

        #endregion Properties
    }
}
