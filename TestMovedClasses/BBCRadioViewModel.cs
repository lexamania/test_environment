using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class BBCRadioViewModel : BBCUrlViewModelBase
	{
		#region Constructors

		public BBCRadioViewModel(MainViewModelBase main) : base(main)
		{
			Title = "BBC Radio";
			SourceType = DataSource.BBCRadio;
			LogoKey = LogoStyleKey.BBCRadioLogo;
			SideLogoKey = LeftSideBarLogoKey.BBCRadioSideLogo;
			Url = Urls.BBC + "/programmes/genres/music";
			BaseUrl = Urls.BBC;
		}

		#endregion Constructors
	}
}