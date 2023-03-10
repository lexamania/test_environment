using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class BrisaMusicViewModel : EmptyViewModel
	{
		#region Constructors

		public BrisaMusicViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Brisa music";
			SourceType = DataSource.BrisaMusic;
			LogoKey = LogoStyleKey.BrisaMusicLogo;
			SideLogoKey = LeftSideBarLogoKey.BrisaMusicSideLogo;
			BaseUrl = "https://brisamusic.com.br/";
		}

		#endregion Constructors
	}
}