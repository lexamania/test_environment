using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class GarminViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public GarminViewModel(MainViewModelBase m) : base(m, MessageBoxText.GarminHelp)
		{
			Title = "Garmin";
			SourceType = DataSource.Garmin;
			LogoKey = LogoStyleKey.GarminLogo;
		}

		#endregion Constructors
	}
}