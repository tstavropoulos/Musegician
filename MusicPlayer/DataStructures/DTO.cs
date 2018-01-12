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

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
            }
        }

        private long _id;
        public long ID
        {
            get { return _id; }
            set
            {
                _id = value;
            }
        }

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
