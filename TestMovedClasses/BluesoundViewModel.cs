using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class BluesoundViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public BluesoundViewModel(MainViewModelBase m) : base(m, MessageBoxText.BluesoundHelp)
		{
			Title = "Bluesound";
			SourceType = DataSource.Bluesound;
			LogoKey = LogoStyleKey.BluesoundLogo;
		}

		#endregion Constructors
	}
}