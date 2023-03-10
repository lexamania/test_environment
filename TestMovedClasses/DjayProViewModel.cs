using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class DjayProViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public DjayProViewModel(MainViewModelBase m) : base(m, MessageBoxText.DjayProHelp)
		{
			Title = "Djay Pro";
			SourceType = DataSource.DjayPro;
			LogoKey = LogoStyleKey.DjayProLogo;
		}

		#endregion Constructors
	}
}