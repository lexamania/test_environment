using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class MovistarViewModel: EmptyViewModel
	{
		#region InnerMethods

		public MovistarViewModel(MainViewModelBase m): base(m)
		{
			Title = "Movistar";
			SourceType = DataSource.Movistar;
			LogoKey = LogoStyleKey.MovistarLogo;
			SideLogoKey = LeftSideBarLogoKey.MovistarSideLogo;
			BaseUrl = "https://www.movistar.com/";
		}

		#endregion InnerMethods
	}
}