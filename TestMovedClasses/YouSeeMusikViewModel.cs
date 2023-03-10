using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class YouSeeMusikViewModel: EmptyViewModel
	{
		#region InnerMethods

		public YouSeeMusikViewModel(MainViewModelBase m): base(m)
		{
			Title = "YouSee Musik";
			SourceType = DataSource.YouSeeMusik;
			LogoKey = LogoStyleKey.YouSeeMusikLogo;
			SideLogoKey = LeftSideBarLogoKey.YouSeeMusikSideLogo;
			BaseUrl = "https://musik.yousee.dk/";
		}

		#endregion InnerMethods
	}
}