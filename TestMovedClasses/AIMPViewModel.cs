using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class AIMPViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public AIMPViewModel(MainViewModelBase m) : base(m, MessageBoxText.AIMPHelp)
		{
			Title = "AIMP";
			SourceType = DataSource.AIMP;
			LogoKey = LogoStyleKey.AimpLogo;
		}

		#endregion Constructors
	}
}