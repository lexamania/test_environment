using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class _7DigitalViewModel : EmptyViewModel
	{
		#region Constructors

		public _7DigitalViewModel(MainViewModelBase m) : base(m)
		{
			Title = "7Digital";
			SourceType = DataSource._7Digital;
			LogoKey = LogoStyleKey._7DigitalLogo;
			SideLogoKey = LeftSideBarLogoKey._7DigitalSideLogo;
			BaseUrl = "https://www.7digital.com/";
		}

		#endregion Constructors
	}
}