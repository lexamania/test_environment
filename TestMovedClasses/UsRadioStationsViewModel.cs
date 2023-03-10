using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class UsRadioStationsViewModel : RadioStationsViewModelBase
	{
		#region Constructors

        public UsRadioStationsViewModel(MainViewModelBase m) : base(m)
        {
            Title = "US Radio Stations";
            Model = new UsRadioStationsModel();
            SourceType = DataSource.UsRadioStations;
            LogoKey = LogoStyleKey.UsRadioStationsLogo;
            SideLogoKey = LeftSideBarLogoKey.UsRadioStationsSideLogo;
        }

		#endregion Constructors
	}
}