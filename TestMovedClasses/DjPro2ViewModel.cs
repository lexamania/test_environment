using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class DjPro2ViewModel : TransferOnlyViewModel
	{
		#region Constructors

		public DjPro2ViewModel(MainViewModelBase m) : base(m, MessageBoxText.DjPro2Help)
		{
			Title = "DJ Pro 2";
			SourceType = DataSource.DjPro2;
			LogoKey = LogoStyleKey.DjPro2Logo;
		}

		#endregion Constructors
	}
}