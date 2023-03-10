using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class LocalScanMp3ViewModel : LocalScanViewModelBase
	{
		#region Constructors

        public LocalScanMp3ViewModel(MainViewModelBase main) : base(main)
        {
            Title = "Local MP3";
            SourceType = DataSource.LocalScan;
            LogoKey = LogoStyleKey.mp3Logo;
            SideLogoKey = LeftSideBarLogoKey.mp3SideLogo;
        }

		#endregion Constructors
	}
}