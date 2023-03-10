using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    [WebUrlParser]
    public class HBOMaxUrlViewModel : MovieServiceUrlViewModelBase
	{
		#region Constructors

        public HBOMaxUrlViewModel(MainViewModelBase m) : base(m, new HBOMaxUrlModel())
        {
            Title = "HBOMaxUrl";
            SourceType = DataSource.HBOMaxUrl;
            LogoKey = LogoStyleKey.HBOMaxLogo;
            SideLogoKey = LeftSideBarLogoKey.HBOMaxUrlSideLogo;
            Url = "https://www.hbomax.com/feature";
        }

		#endregion Constructors
	}
}