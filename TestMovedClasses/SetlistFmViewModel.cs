using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class SetlistFmViewModel: EmptyViewModel
	{
		#region InnerMethods

		public SetlistFmViewModel(MainViewModelBase m): base(m)
		{
			Title = "Setlist.fm";
			SourceType = DataSource.SetlistFm;
			LogoKey = LogoStyleKey.SetlistFmLogo;
			SideLogoKey = LeftSideBarLogoKey.SetlistFmSideLogo;
			BaseUrl = "https://www.setlist.fm/";
		}

		#endregion InnerMethods
	}
}