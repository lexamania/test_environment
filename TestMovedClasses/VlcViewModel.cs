using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class VlcViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public VlcViewModel(MainViewModelBase m) : base(m, MessageBoxText.VlcHelp)
		{
			Title = "VLC media player";
			SourceType = DataSource.VLC;
			LogoKey = LogoStyleKey.VlcLogo;
		}

		#endregion Constructors
	}
}