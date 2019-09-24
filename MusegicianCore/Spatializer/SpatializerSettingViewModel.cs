using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Spatializer
{
    public class SpatializerSettingViewModel
    {
        #region Data

        SpatializerSettingDTO data;

        #endregion Data
        #region Constructor

        public SpatializerSettingViewModel(SpatializerSettingDTO data)
        {
            this.data = data;

            if (data.name == "Custom")
            {
                Hidden = true;
            }
        }

        #endregion Constructor
        #region Properties

        public string Name => data.name;
        public IR_Position[,] Position => data.position;
        public bool Hidden { get; set; }

        #endregion Properties
    }
}
