using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Deredundafier
{
    public class SelectorViewModel : DeredundafierViewModel
    {
        #region Constructor

        public SelectorViewModel(SelectorDTO data, PotentialMatchViewModel parent)
            : base(data, parent)
        {
            foreach (DeredundafierDTO child in data.Children)
            {
                Children.Add(new PassiveViewModel(child, this));
            }
        }

        #endregion Constructor
        #region Properties

        public new SelectorDTO Data => base.Data as SelectorDTO;
        public new PotentialMatchViewModel Parent => base.Parent as PotentialMatchViewModel;

        public bool IsChecked
        {
            get => Data.IsChecked;
            set
            {
                if (IsChecked != value)
                {
                    Data.IsChecked = value;
                    OnPropertyChanged("IsChecked");
                    Parent.ReevaluateColor();
                }
            }
        }

        #endregion Properties
        #region Presentation Members

        public override bool IsGrayedOut => false;

        #endregion Presentation Members
    }
}
