using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class SamsungMusicViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public SamsungMusicViewModel(MainViewModelBase m) : base(m, MessageBoxText.SamsungMusicHelp)
		{
			Title = "Samsung Music";
			SourceType = DataSource.SamsungMusic;
			LogoKey = LogoStyleKey.SamsungMusicLogo;
		}

		#endregion Constructors
	}
}