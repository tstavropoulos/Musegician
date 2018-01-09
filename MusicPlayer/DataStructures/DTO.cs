using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.DataStructures
{
    public abstract class DTO
    {
        readonly List<DTO> _children = new List<DTO>();
        public IList<DTO> Children
        {
            get { return _children; }
        }

        public string Name { get; set; }
        public long ID { get; set; }

        private double _weight = double.NaN;
        public double Weight
        {
            get
            {
                if (double.IsNaN(_weight))
                {
                    return DefaultWeight;
                }

                return _weight;
            }
            set { _weight = value; }
        }

        protected virtual double DefaultWeight
        {
            get { return 1.0; }
        }
    }
}
