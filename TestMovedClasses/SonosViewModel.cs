using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class SonosViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public SonosViewModel(MainViewModelBase m) : base(m, MessageBoxText.SonosHelp)
		{
			Title = "Sonos";
			SourceType = DataSource.Sonos;
			LogoKey = LogoStyleKey.SonosLogo;
		}

		#endregion Constructors
	}
}