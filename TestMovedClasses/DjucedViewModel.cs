using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class DjucedViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public DjucedViewModel(MainViewModelBase m) : base(m, MessageBoxText.DjucedHowTo)
		{
			Title = "Djuced";
			SourceType = DataSource.Djuced;
			LogoKey = LogoStyleKey.DjucedLogo;
		}

		#endregion Constructors
	}
}