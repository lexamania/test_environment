using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class LocalScanViewModel : LocalScanViewModelBase
	{
		#region Constructors

        public LocalScanViewModel(MainViewModelBase main) : base(main)
        {
            Title = "Local Music";
            SourceType = DataSource.LocalScan;
            LogoKey = LogoStyleKey.LocalMusicLogo;
            SideLogoKey = LeftSideBarLogoKey.LocalMusicSideLogo;
        }

		#endregion Constructors
	}
}