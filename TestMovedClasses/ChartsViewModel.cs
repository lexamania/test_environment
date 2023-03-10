using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class ChartsViewModel : SectionViewModelBase
	{
		#region Fields

		private TabViewModelBase _selectedChartService;
		private bool isAuthenticated;
		private bool IsLoadApple;
		private SpotifyModel spotifyModel;
		private SpotifyViewModel spotifyViewModel;
		private ExploreModel _model;

		#endregion Fields

		#region Constructors

		public ChartsViewModel(MainViewModelBase m) : base(m)
		{
			Title = Texts.ChartsServiceTitle;
			Model = new ChartsModel();
			_model = new ExploreModel(m);
			SourceType = DataSource.Charts;
			LogoKey = LogoStyleKey.ChartsLogo;
			CurrentVMType = VmType.Service;
			RegState = RegistrationState.Needless;

			#region Commands

			//for now can't send user to song pages in tiktok ,shazam, etc
			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTikTokTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
			};

			#endregion Commands

			#region Tabs

			//https://portal.organicfruitapps.com/programming-guides/v2/us_en-us/featured-playlists.json - has all playlist links 
			var appleMusicChartsTab = new PlaylistTabViewModelBase(m, "Apple Music", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Apple), commandTracks);

			//spotify takes playlists from your account, not from web
			var spotifyChartsTab = new PlaylistTabViewModelBase(m, TabsKey.Spotify.ToString(), LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Spotify), commandTracks);

			//scrapes chart names from https://www.billboard.com/charts and adds them to the same link
			var billboardTab = new PlaylistTabViewModelBase(m, TabsKey.Billboard.ToString(), LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Billboard), commandTracks);

			//https://tokboard.com/
			var tikTokTab = new TrackTabViewModelBase(m, "Weekly TikTok", LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTikTokTab,
				new Initial_TaskItem("Reload", Initial_Update_TikTokWeekly), EmptyCommandsBase);

			#endregion Tabs

			Tabs.Add(appleMusicChartsTab);
			Tabs.Add(spotifyChartsTab);
			Tabs.Add(billboardTab);
			Tabs.Add(tikTokTab);
		}

		#endregion Constructors

		#region AuthMethods

		public void SelectService(TabViewModelBase service)
		{
			MainViewModel.NavigateTo(NavigationKeysChild.ChartsContentPage);
			SelectedTab = service;
			if (SelectedTab.EmptyTabText != "")
				SelectedTab.Loading = true;
		}

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NavigateTo(NavigationKeysChild.ChartsPage);
				foreach (var tab in Tabs)
				{
					_selectedChartService = tab;
					await tab.InitialMethod.Initial_DoWork();
				}
				_selectedChartService = null;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}

			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

		private async Task<bool> Initial_Update_Spotify(bool forceUpdate = false)
		{
			var tab = _selectedChartService ?? SelectedTab;
			if (tab?.MediaItems.Count != 0 && !forceUpdate)
			{
				return true;
			}
			tab.Loading = true;
			tab.MediaItems.Clear();
			var playlists = await _model.GetSpotifyPlaylists();
			tab.MediaItems.AddRange(playlists);
			tab.Loading = false;
			return true;
		}

		private async Task<bool> Initial_Update_Apple(bool forceUpdate = false)
		{
			var tab = _selectedChartService ?? SelectedTab;
			if (tab.MediaItems.Count != 0 && !forceUpdate)
			{
				return true;
			}
			tab.Loading = true;
			tab.MediaItems?.Clear();
			var playlists = await _model.GetApplePlaylists().ConfigureAwait(false);
			await Dispatcher.UIThread.InvokeAsync(() => tab.MediaItems?.AddRange(playlists));
			tab.Loading = false;
			return true;
		}

		private async Task<bool> Initial_Update_Billboard(bool forceUpdate = false)
		{
			var tab = _selectedChartService ?? SelectedTab;
			SelectedTab.LoadingText = MusConvConfig.PlayListLoading;
			if (tab.MediaItems.Count != 0 && !forceUpdate)
			{
				return true;
			}
			tab.Loading = true;
			tab.MediaItems?.Clear();
			var playlists = await _model.GetBillBoardPlaylists().ConfigureAwait(false);
			await Dispatcher.UIThread.InvokeAsync(() => tab.MediaItems?.AddRange(playlists));
			tab.Loading = false;
			return true;
		}

		private async Task<bool> Initial_Update_TikTokWeekly(bool forceUpdate = false)
		{
			var tab = _selectedChartService ?? SelectedTab;
			SelectedTab.LoadingText = MusConvConfig.SongsLoading;
			if (tab.MediaItems.Count != 0 && !forceUpdate)
			{
				return true;
			}

			tab.Loading = true;
			tab?.MediaItems?.Clear();
			var tracks = await _model.GetTikTokTracks();
			await Dispatcher.UIThread.InvokeAsync(() => tab.MediaItems?.AddRange(tracks));
			tab.Loading = false;
			return true;
		}

		#endregion InitialUpdate

		#region InnerMethods

		public ObservableCollection<TabViewModelBase> ChartTabs { get; set; } = new ObservableCollection<TabViewModelBase>();

		public void ChartsServiсeImage(TabViewModelBase service)
		{
			switch (service.Title)
			{
				case "Spotify":
					service.ServiceImage = "avares://MusConv/Assets/ChartsLogo/featured-playlists-spotify.png";
					break;
				case "Apple Music":
					service.ServiceImage = "avares://MusConv/Assets/ChartsLogo/AppleMusic-charts-logo.jpg";
					break;
				case "Billboard":
					service.ServiceImage = "avares://MusConv/Assets/ChartsLogo/Billboard-charts-logo.jpg";
					break;
				case "Weekly TikTok":
					service.ServiceImage = "avares://MusConv/Assets/ChartsLogo/top-weekly-tiktok.png";
					break;
				default: service.ServiceImage = string.Empty;
					break;
			}		  
		}

		#endregion InnerMethods
	}

}