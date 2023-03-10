using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Lib.BillboardUrl.Enums;
using System.Collections.ObjectModel;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class BillBoardViewModel:SectionViewModelBase
	{
		#region Fields

		private readonly TabViewModelBase topChartsTab;
		private readonly TabViewModelBase greatestOfAllTimeTab;
		private readonly TabViewModelBase hitsOfTheWorldTab;
		private readonly TabViewModelBase rockTab;
		private readonly TabViewModelBase popTab;
		private readonly TabViewModelBase electronicTab;
		private readonly TabViewModelBase countryTab;
		private readonly TabViewModelBase songsOfTheSummerTab;
		private readonly TabViewModelBase webTab;
		private readonly TabViewModelBase artistsTab;
		private readonly TabViewModelBase groupsTab;
		private readonly TabViewModelBase albumsTab;
		private BillBoardModel BillBoardModel => Model as BillBoardModel;
		public bool IsLoading { get; set; }
		public string LoadingText => MusConvConfig.ChartsLoading;

		#endregion Fields

		#region DataProperties

		public ObservableCollection<TabViewModelBase> GeneralTabs { get; set; } = new();

		public ObservableCollection<TabViewModelBase> SpecificTabs { get; set; } = new();

		public ObservableCollection<TabViewModelBase> ArtistsTabs { get; set; } = new();

		#endregion DataProperties

		#region AccountsProperties

		public ObservableCollection<TabViewModelBase> GeneralTabs { get; set; } = new();

		public ObservableCollection<TabViewModelBase> SpecificTabs { get; set; } = new();

		public ObservableCollection<TabViewModelBase> ArtistsTabs { get; set; } = new();

		#endregion AccountsProperties

		#region MainProperties

		public ObservableCollection<TabViewModelBase> GeneralTabs { get; set; } = new();

		public ObservableCollection<TabViewModelBase> SpecificTabs { get; set; } = new();

		public ObservableCollection<TabViewModelBase> ArtistsTabs { get; set; } = new();

		#endregion MainProperties

		#region ItemsProperties

		public ObservableCollection<TabViewModelBase> GeneralTabs { get; set; } = new();

		public ObservableCollection<TabViewModelBase> SpecificTabs { get; set; } = new();

		public ObservableCollection<TabViewModelBase> ArtistsTabs { get; set; } = new();

		#endregion ItemsProperties

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			MainViewModel.NavigateTo(NavigationKeysChild.BillBoardPage);

			if (GeneralTabs.Any(x => string.IsNullOrEmpty(x.ServiceImage)))
				await InitializeCovers();

			return true;
		}

        public async Task SelectService(TabViewModelBase service)
        {
            SelectedTab = service;
            MainViewModel.NavigateTo(NavigationKeysChild.ChartsContentPage);
            await service.InitialMethod.Initial_DoWork();
        }

        private async Task InitializeCovers()
        {
			IsLoading = true;

			var generalCovers = await BillBoardModel.GetTabsCovers(TabType.General);
			var specificCovers = await BillBoardModel.GetTabsCovers(TabType.Specific);
			var artistsCovers = await BillBoardModel.GetTabsCovers(TabType.Artists);

			int i = 0;
			GeneralTabs.ToList().ForEach(item => item.ServiceImage = generalCovers[i++]);

			i = 0;
			SpecificTabs.ToList().ForEach(item => item.ServiceImage = specificCovers[i++]);

			i = 0;
			ArtistsTabs.ToList().ForEach(item => item.ServiceImage = artistsCovers[i++]);

			IsLoading = false;
		}

		#endregion AuthMethods

		#region InitialUpdate

		private Task<bool> Initial_Update_TopCharts(bool arg)
		{
			return InitialUpdateBuilder(() => BillBoardModel.ParsePlaylistLinks(ChartsCategory.TopCharts), SelectedTab, arg);
		}

		private Task<bool> Initial_Update_BillBoard200(bool arg)
		{
			return InitialUpdateBuilder(() => Model.GetAlbums(), SelectedTab, arg);
		}

		private Task<bool> Initial_Update_Social50(bool arg)
		{
			return InitialUpdateBuilder(() => BillBoardModel.ParseArtistsLinks(ArtistsCategory.Social50), SelectedTab, arg);
		}

		private Task<bool> Initial_Update_Artists100(bool arg)
		{
			return InitialUpdateBuilder(() => BillBoardModel.ParseArtistsLinks(ArtistsCategory.Artists100), SelectedTab, arg);
		}

		private Task<bool> Initial_Update_Web(bool arg)
        {
			return InitialUpdateBuilder(() => BillBoardModel.ParsePlaylistLinks(ChartsCategory.Web), SelectedTab, arg);
		}

        private Task<bool> Initial_Update_SongsOfTheSummer(bool arg)
        {
			return InitialUpdateBuilder(() => BillBoardModel.ParsePlaylistLinks(ChartsCategory.SongsOfTheSummer), SelectedTab, arg);
        }

		private Task<bool> Initial_Update_Country(bool arg)
		{
			return InitialUpdateBuilder(() => BillBoardModel.ParsePlaylistLinks(ChartsCategory.Country), SelectedTab, arg);
		}

		private Task<bool> Initial_Update_Electronic(bool arg)
		{
			return InitialUpdateBuilder(() => BillBoardModel.ParsePlaylistLinks(ChartsCategory.Electronic), SelectedTab, arg);
		}

		private Task<bool> Initial_Update_Pop(bool arg)
		{
			return InitialUpdateBuilder(() => BillBoardModel.ParsePlaylistLinks(ChartsCategory.Pop), SelectedTab, arg);
		}

		private Task<bool> Initial_Update_Rock(bool arg)
		{
			return InitialUpdateBuilder(() => BillBoardModel.ParsePlaylistLinks(ChartsCategory.Rock), SelectedTab, arg);
		}

		private Task<bool> Initial_Update_HitsOfTheWorld(bool arg)
        {
			return InitialUpdateBuilder(() => BillBoardModel.ParsePlaylistLinks(ChartsCategory.HitsOfTheWorld), SelectedTab, arg);
		}

        private Task<bool> Initial_Update_GreatestOfAllTime(bool arg)
        {
			return InitialUpdateBuilder(() => BillBoardModel.ParsePlaylistLinks(ChartsCategory.GreatestOfAllTime), SelectedTab, arg);
        }

		#endregion InitialUpdate

		#region Commands

		public override Task Command_OpenTracksPage(object arg)
		{
			var tab = Tabs.Concat(GeneralTabs).FirstOrDefault(ot => ot.MediaItems.Any(mi => mi.Equals(arg)));
			SelectedTab = tab;

			return OpenPlaylist(NavigationKeysChild.TracksControl, arg);
		}

		#endregion Commands

		#region InnerMethods

		public BillBoardViewModel(MainViewModelBase main):base(main)
		{
			Title = "BillBoard";
			CurrentVMType = VmType.Service;
			SourceType = DataSource.Billboard;
			LogoKey = LogoStyleKey.BillboardLogo;
			SideLogoKey = LeftSideBarLogoKey.BillboardSideLogo;
			RegState = RegistrationState.Needless;
			Model = new BillBoardModel();
			BaseUrl = "https://www.billboard.com/";

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);
			var commandAlbumsTab = new List<Command_TaskItem>  (AlbumsTabCommandsBase);
			var commandArtistsTab = new List<Command_TaskItem>  (ArtistsTabCommandsBase);

			#endregion Commands

			#region Tabs

			topChartsTab = new PlaylistTabViewModelBase(main, "Top Charts", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_TopCharts), commandTracks);
            greatestOfAllTimeTab = new PlaylistTabViewModelBase(main, "Greatest of all time", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
                new Initial_TaskItem("Reload", Initial_Update_GreatestOfAllTime), commandTracks);
			hitsOfTheWorldTab = new PlaylistTabViewModelBase(main, "Hits of the world", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_HitsOfTheWorld), commandTracks);
			rockTab = new PlaylistTabViewModelBase(main, "Rock", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Rock), commandTracks);
			popTab = new PlaylistTabViewModelBase(main, "Pop", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Pop), commandTracks);
			electronicTab = new PlaylistTabViewModelBase(main, "Dance/Electronic", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Electronic), commandTracks);
			countryTab = new PlaylistTabViewModelBase(main, "Country", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Country), commandTracks);
			songsOfTheSummerTab = new PlaylistTabViewModelBase(main, "Songs of the summer", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_SongsOfTheSummer), commandTracks);
			webTab = new PlaylistTabViewModelBase(main, "Web", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Web), commandTracks);
			artistsTab = new ArtistTabViewModelBase(main, "Artist 100", LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists100), commandTracks);
			groupsTab = new ArtistTabViewModelBase(main, "Social 50", LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Social50), commandTracks);
			albumsTab = new AlbumTabViewModelBase(main, "The Billboard 200", LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_BillBoard200), commandTracks);

			#endregion Tabs

            Tabs.Add(topChartsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(groupsTab);
			Tabs.Add(greatestOfAllTimeTab);
            Tabs.Add(hitsOfTheWorldTab);
            Tabs.Add(rockTab);
            Tabs.Add(popTab);
            Tabs.Add(electronicTab);
            Tabs.Add(countryTab);
            Tabs.Add(songsOfTheSummerTab);
            Tabs.Add(webTab);

			GeneralTabs.Add(topChartsTab);
			GeneralTabs.Add(albumsTab);
			GeneralTabs.Add(greatestOfAllTimeTab);
			GeneralTabs.Add(hitsOfTheWorldTab);
			GeneralTabs.Add(songsOfTheSummerTab);

			SpecificTabs.Add(rockTab);
			SpecificTabs.Add(popTab);
			SpecificTabs.Add(electronicTab);
			SpecificTabs.Add(countryTab);
			SpecificTabs.Add(webTab);

			ArtistsTabs.Add(artistsTab);
			ArtistsTabs.Add(groupsTab);
		}

		public void NavigateToParser()
		{
			MainViewModel.SelectedItem = MainViewModel.SideItems.FirstOrDefault(x => x.Title == "BillboardUrl");
			MainViewModel.NavigateTo(NavigationKeysChild.WebUrl);
		}

		#endregion InnerMethods
	}
}