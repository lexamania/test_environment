using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class UkRadioStationsViewModel : RadioStationsViewModelBase
	{
		#region Constructors

        public UkRadioStationsViewModel(MainViewModelBase m) : base(m)
        {
            Title = "UK Radio Stations";
            Model = new UkRadioStationsModel();
            SourceType = DataSource.UkRadioStations;
            LogoKey = LogoStyleKey.UkRadioStationsLogo;
            SideLogoKey = LeftSideBarLogoKey.UkRadioStationsSideLogo;
        }

		#endregion Constructors
	}
}