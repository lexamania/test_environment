using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class LineMusicViewModel: EmptyViewModel
	{
		#region InnerMethods

		public LineMusicViewModel(MainViewModelBase m): base(m)
		{
			Title = "Line Music";
			SourceType = DataSource.LineMusic;
			LogoKey = LogoStyleKey.LineMusicLogo;
			SideLogoKey = LeftSideBarLogoKey.LineMusicSideLogo;
			BaseUrl = "https://music.line.me/";
		}

		#endregion InnerMethods
	}
}