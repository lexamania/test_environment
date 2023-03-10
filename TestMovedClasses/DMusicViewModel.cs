using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class DMusicViewModel: EmptyViewModel
	{
		#region InnerMethods

		public DMusicViewModel(MainViewModelBase m): base(m)
		{
			Title = "DMusic";
			SourceType = DataSource.DMusic;
			LogoKey = LogoStyleKey.DMusicLogo;
			SideLogoKey = LeftSideBarLogoKey.DMusicSideLogo;
			BaseUrl = "https://web.digicelmusic.com/index.html";
		}

		#endregion InnerMethods
	}
}