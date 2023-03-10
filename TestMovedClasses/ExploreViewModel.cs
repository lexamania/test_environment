using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using PropertyChanged;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[AddINotifyPropertyChangedInterface]
	public class ExploreViewModel : SectionViewModelBase
	{
		#region Fields

		private ExploreModel ExploreModel => Model as ExploreModel;
		private readonly TabViewModelBase _recentTransferredTab;
		private readonly TabViewModelBase _spotifyChartsTab;
		private readonly TabViewModelBase _appleMusicChartsTab;
		private readonly TabViewModelBase _billboardTab;
		private readonly TabViewModelBase _tikTokTab;
		public ObservableCollection<TabViewModelBase> ChartTabs { get; set; }
		public ObservableCollection<TabViewModelBase> OtherTabs { get; set; }

		#endregion Fields

		#region Constructors

		public ExploreViewModel(MainViewModelBase m) : base(m)
		{
			Title = Texts.ExploreServiceTitle;
			Model = new ExploreModel(m);
			SourceType = DataSource.Explore;
			LogoKey = LogoStyleKey.ExploreLogo;
			SideLogoKey = LeftSideBarLogoKey.ExploreSideLogo;
			CurrentVMType = VmType.Service;
			RegState = RegistrationState.Needless;
			
			ChartTabs = new ObservableCollection<TabViewModelBase>();
			OtherTabs = new ObservableCollection<TabViewModelBase>();

			#region Commands

			//for now can't send user to song pages in tiktok ,shazam, etc
			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnSpotifyCommand(CommandSpotifyTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
			};

			//for now can't send user to song pages in tiktok ,shazam, etc
			var commandAppleTracks = new List<Command_TaskItem>  (TracksCommandsBase)
			{
				new ViewOnAppleMusicCommand(CommandAppleTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandTikTokTabs = new List<Command_TaskItem> (TracksTabCommandsBase);

			#endregion Commands

			#region Tabs

			_recentTransferredTab = new PlaylistTabViewModelBase(m, AppTabs.RecentTransfers, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_RecentTransferred), commandTracks);

			//https://portal.organicfruitapps.com/programming-guides/v2/us_en-us/featured-playlists.json - has all playlist links 
			_appleMusicChartsTab = new PlaylistTabViewModelBase(m, "Apple Music", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Apple), commandAppleTracks);

			_spotifyChartsTab = new PlaylistTabViewModelBase(m, TabsKey.Spotify.ToString(), LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Spotify), commandTracks);
	
			//scrapes chart names from https://www.billboard.com/charts and adds them to the same link
			_billboardTab = new PlaylistTabViewModelBase(m, TabsKey.Billboard.ToString(), LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Billboard), commandTracks);

			//https://tokboard.com/
			_tikTokTab = new TrackTabViewModelBase(m, "Weekly TikTok", LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTikTokTabs,
				new Initial_TaskItem("Reload", Initial_Update_TikTokWeekly), commandTracks);

			_spotifyChartsTab.ServiceImage = "avares://MusConv/Assets/ChartsLogo/featured-playlists-spotify.png";
			_appleMusicChartsTab.ServiceImage = "avares://MusConv/Assets/ChartsLogo/AppleMusic-charts-logo.jpg";
			_billboardTab.ServiceImage = "avares://MusConv/Assets/ChartsLogo/Billboard-charts-logo.jpg";
			_tikTokTab.ServiceImage = "avares://MusConv/Assets/ChartsLogo/top-weekly-tiktok.png";

			#endregion Tabs

			Tabs.Add(_appleMusicChartsTab);
			Tabs.Add(_spotifyChartsTab);
			Tabs.Add(_billboardTab);
			Tabs.Add(_tikTokTab);
			ChartTabs.Add(_appleMusicChartsTab);
			ChartTabs.Add(_spotifyChartsTab);
			ChartTabs.Add(_billboardTab);
			ChartTabs.Add(_tikTokTab);
			Tabs.Add(_recentTransferredTab);
			OtherTabs.Add(_recentTransferredTab);
			OtherTabs.Add(_spotifyChartsTab);
			OtherTabs.Add(_appleMusicChartsTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NavigateTo(NavigationKeysChild.ExplorePage);
				var tasks = OtherTabs.Select(tab => tab.InitialMethod.Initial_DoWork()).ToList();
				await Task.WhenAll(tasks);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
			return true;
		}

		public async Task SelectService(TabViewModelBase service)
		{
			SelectedTab = service;
			MainViewModel.NavigateTo(NavigationKeysChild.ChartsContentPage);
			await service.InitialMethod.Initial_DoWork();
		}

		#endregion AuthMethods

		#region InitialUpdate

		private Task<bool> Initial_Update_RecentTransferred(bool forceUpdate = false)
		{
			return Initial_Update_Tab(_recentTransferredTab, ExploreModel.GetRecentTransferred(), forceUpdate);
		}

		private Task<bool> Initial_Update_Spotify(bool forceUpdate = false)
		{
			return Initial_Update_Tab(_spotifyChartsTab, ExploreModel.GetSpotifyPlaylists(), forceUpdate);
		}

		private Task<bool> Initial_Update_Apple(bool forceUpdate = false)
		{
			return Initial_Update_Tab(_appleMusicChartsTab, ExploreModel.GetApplePlaylists(), forceUpdate);
		}

		private Task<bool> Initial_Update_Billboard(bool forceUpdate = false)
		{
			return Initial_Update_Tab(_billboardTab, ExploreModel.GetBillBoardPlaylists(), forceUpdate);
		}

		private Task<bool> Initial_Update_TikTokWeekly(bool forceUpdate = false)
		{
			return Initial_Update_Tab(_tikTokTab, ExploreModel.GetTikTokTracks(), forceUpdate);
		}

		private async Task<bool> Initial_Update_Tab<T>(TabViewModelBase tab, Task<List<T>> updateTask, bool forceUpdate = false) where T: MusConvItemBase
		{
			if (tab.MediaItems.Count != 0 && !forceUpdate || tab.Loading)
			{
				return true;
			}
			
			tab.Loading = true;
			SelectedTab.LoadingText = MusConvConfig.PlayListLoading;
			try
			{
				tab.MediaItems.Clear();
				var items = await updateTask;
				tab.MediaItems.AddRange(items);
				return true;
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				return false;
			}
			finally
			{
				tab.Loading = false;
			}
		}

		#endregion InitialUpdate

		#region Commands

		public override Task Command_OpenTracksPage(object arg)
		{
			var tab = OtherTabs.Concat(Tabs).Concat(ChartTabs).FirstOrDefault(ot => ot.MediaItems.Any(mi => mi.Equals(arg))); 
			SelectedTab = tab;

			return OpenPlaylist(NavigationKeysChild.TracksControl, arg);
		}

		#endregion Commands

		#region OpeningMethods

		public Task CommandSpotifyTrack_Open(object track)
		{
			try
			{
				var spotifyTrack = track as List<MusConvModelBase>;
				return ExploreModel.OpenSpotifyTrack(spotifyTrack.First() as MusConvTrack);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return Task.FromResult(false);
			}
		}

		public Task CommandAppleTrack_Open(object arg)
		{
			var items = arg as List<MusConvModelBase>;
			try
			{
				return ExploreModel.OpenAppleTrack(items[0] as MusConvTrack);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return Task.FromResult(false);
			}
		}

		#endregion OpeningMethods
	}
}