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
        bool _reentryBlock = false;
        bool _isThreeState = false;

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
                if (_checked != value && !_reentryBlock)
                {
                    _reentryBlock = true;
                    _checked = value;
                    IsThreeState = !value.HasValue;
                    UpdateChildrenCheckState();
                    _reentryBlock = false;
                    OnPropertyChanged("ChildrenSelected");
                }
            }
        }

        public bool IsThreeState
        {
            get { return _isThreeState; }
            private set
            {
                if (_isThreeState != value)
                {
                    _isThreeState = value;
                    OnPropertyChanged("IsThreeState");
                }
            }
        }

        public override bool IsGrayedOut
        {
            get { return false; }
        }

        #endregion Presentation Members
        #region Data Methods

        private void UpdateChildrenCheckState()
        {
            if (_checked.HasValue)
            {
                foreach (SelectorViewModel selector in Children)
                {
                    selector.IsChecked = _checked.Value;
                }
            }
        }

        private bool? DetermineCheckState()
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
                return true;
            }
            else if (engagedCount == 0)
            {
                return false;
            }

            return null;
        }

        #endregion Data Methods
        #region Child Methods

        public void ReevaluateColor()
        {
            if (_reentryBlock)
            {
                return;
            }

            ChildrenSelected = DetermineCheckState();
        }

        #endregion Child Methods
    }
}
