using System.Linq;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.Models.MusicService.MusConvServiceModels;
using System;

namespace MusConv.ViewModels.ViewModels
{
    public class GlobalSearchViewModel : SectionViewModelBase
	{
		#region Constructors

        public GlobalSearchViewModel(MainViewModel m) : base(m)
        {
            Title = "Global Search";
            RegState = RegistrationState.Needless;
            SourceType = DataSource.GlobalSearch;
            LogoKey = LogoStyleKey.GlobalSearchLogo;
            SideLogoKey = LeftSideBarLogoKey.GlobalSearchSideLogo;
            Model = new GlobalSearchModel(MainViewModel.SideItems
                .SelectMany(x => x.Tabs.Select(x => x.MediaItems)));

            var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, new(),
                new(),
                new Initial_TaskItem("Reload", Initial_Update_Tracks), new(),
                new LogOut_TaskItem("LogOut", Log_Out));

            Tabs.Add(tracksTab);
        }

		#endregion Constructors

		#region AuthMethods

        public override Task<bool> IsServiceSelectedAsync()
        {
            MainViewModel.NavigateTo(NavigationKeysChild.GlobalSearchPage);
            
            SelectedTab.MediaItems.Clear();
            return Initial_Update_Tracks(true);
        }

		#endregion AuthMethods
	}
}