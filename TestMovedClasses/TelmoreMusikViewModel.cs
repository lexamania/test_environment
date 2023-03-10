using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class TelmoreMusikViewModel: EmptyViewModel
	{
		#region InnerMethods

		public TelmoreMusikViewModel(MainViewModelBase m): base(m)
		{
			Title = "Telmore Musik";
			SourceType = DataSource.TelmoreMusik;
			LogoKey = LogoStyleKey.TelmoreMusikLogo;
			SideLogoKey = LeftSideBarLogoKey.TelmoreMusikSideLogo;
			BaseUrl = "https://musik.telmore.dk/";
		}

		#endregion InnerMethods
	}
}