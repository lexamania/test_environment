using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class MediaMonkeyViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public MediaMonkeyViewModel(MainViewModelBase m) : base(m, MessageBoxText.MediaMonkeyHelp)
		{
			Title = "MediaMonkey";
			SourceType = DataSource.MediaMonkey;
			LogoKey = LogoStyleKey.MediaMonkeyLogo;
			BaseUrl = "https://www.mediamonkey.com/";
		}

		#endregion Constructors
	}
}