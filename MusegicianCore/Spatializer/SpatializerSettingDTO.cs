using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Spatializer
{
    public class SpatializerSettingDTO
    {
        #region Data

        public IR_Position[,] position = new IR_Position[2, 2]
        {
            {IR_Position.IR_n45, IR_Position.IR_n45},
            {IR_Position.IR_p45, IR_Position.IR_p45}
        };
        public string name = "";

        #endregion Data
    }
}
