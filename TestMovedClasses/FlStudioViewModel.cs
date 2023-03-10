using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class FlStudioViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public FlStudioViewModel(MainViewModelBase m) : base(m, MessageBoxText.FlStudioHelp)
		{
			Title = "FL Studio";
			SourceType = DataSource.FlStudio;
			LogoKey = LogoStyleKey.Blank;
		}

		#endregion Constructors
	}
}