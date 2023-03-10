using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class BBCSoundsUrlViewModel : BBCUrlViewModelBase
	{
		#region Constructors

        public BBCSoundsUrlViewModel(MainViewModelBase m) : base(m)
        {
            Title = "BBCSoundsUrl";
            SourceType = DataSource.BBCSoundsUrl;
            LogoKey = LogoStyleKey.BBCSoundsUrlLogo;
            SideLogoKey = LeftSideBarLogoKey.BBCSoundsUrlSideLogo;
            Url = Urls.BBCSoundsUrl;
        }

		#endregion Constructors
	}
}