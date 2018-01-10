using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Deredundafier
{
    public class PotentialMatchViewModel : DeredundafierViewModel
    {
        #region Data

        bool? _checked = false;

        #endregion Data
        #region Constructor

        public PotentialMatchViewModel(DeredundafierDTO data)
            : base(data, null)
        {
            foreach (SelectorDTO child in data.Children)
            {
                Children.Add(new SelectorViewModel(child, this));
            }
        }

        #endregion Constructor
        #region Presentation Members

        public bool? ChildrenSelected
        {
            get { return _checked; }
            set
            {
                if (_checked != value && value.HasValue)
                {
                    foreach (SelectorViewModel selector in Children)
                    {
                        selector.IsChecked = value.Value;
                    }
                }
            }
        }

        public override bool IsGrayedOut
        {
            get { return false; }
        }

        #endregion Presentation Members
        #region Child Methods

        public void ReevaluateColor()
        {
            int engagedCount = 0;
            int disengagedCount = 0;

            foreach (SelectorViewModel selector in Children)
            {
                if (selector.IsChecked)
                {
                    ++engagedCount;
                }
                else
                {
                    ++disengagedCount;
                }
            }

            if (disengagedCount == 0)
            {
                _checked = true;
                OnPropertyChanged("ChildrenSelected");
            }
            else if (engagedCount == 0)
            {
                _checked = false;
                OnPropertyChanged("ChildrenSelected");
            }
            else
            {
                _checked = null;
                OnPropertyChanged("ChildrenSelected");
            }
        }

        #endregion Child Methods
    }
}
