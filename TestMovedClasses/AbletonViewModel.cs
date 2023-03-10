using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class AbletonViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public AbletonViewModel(MainViewModelBase m) : base(m, MessageBoxText.AbletonHelp)
		{
			Title = "Ableton";
			SourceType = DataSource.Ableton;
			LogoKey = LogoStyleKey.AbletonLogo;
			SideLogoKey = LeftSideBarLogoKey.AbletonSideLogo;
		}

		#endregion Constructors
	}
}