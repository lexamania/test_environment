using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	internal class You42ViewModel : EmptyViewModel
	{
		#region Constructors

		public You42ViewModel(MainViewModelBase m) : base(m)
		{
			Title = "You42";
			SourceType = DataSource.You42;
			LogoKey = LogoStyleKey.You42Logo;
			SideLogoKey = LeftSideBarLogoKey.You42SideLogo;
			BaseUrl = "https://www.you42.com/home";
		}

		#endregion Constructors
	}
}