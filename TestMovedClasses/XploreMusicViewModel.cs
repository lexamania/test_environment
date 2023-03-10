using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class XploreMusicViewModel: EmptyViewModel
	{
		#region InnerMethods

		public XploreMusicViewModel(MainViewModelBase m): base(m)
		{
			Title = "Xplore Music";
			SourceType = DataSource.XploreMusic;
			LogoKey = LogoStyleKey.XploreMusicLogo;
			SideLogoKey = LeftSideBarLogoKey.XploreMusicSideLogo;
			BaseUrl = "https://www.a1.by/";
		}

		#endregion InnerMethods
	}
}