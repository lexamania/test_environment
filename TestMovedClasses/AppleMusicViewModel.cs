using Avalonia.Threading;
using MusConv.Abstractions.Extensions;
using MusConv.Lib.Apple;
using MusConv.Lib.Apple.Exceptions;
using MusConv.Lib.Apple.Models;
using MusConv.MessageBoxManager.Texts;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Sentry.Extensions;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Settings;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using Nancy.Extensions;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.Api;
using MusConv.ViewModels.Interfaces;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class AppleMusicViewModel : SectionViewModelBase, IHighlightableCommands
	{
		#region Fields

		private readonly StatsViewModelBase _statsTab;
		private readonly StatsCategoryTab _artistsStatsTab;
		private readonly StatsCategoryTab _tracksStatsTab;
		private readonly StatsCategoryTab _albumsStatsTab;
		private readonly StatsCategoryTab _genresStatsTab;
		private readonly TilesTabViewModelBase _recentlyAddedTab;
		public AppleModel AppleModel => Model as AppleModel;
        public Dictionary<TabViewModelBase, Command_TaskItem[]> HighlightableCommands { get; }

		#endregion Fields

		#region Constructors

        public AppleMusicViewModel(MainViewModelBase m) : base(m)
		{
			Url = Urls.AppleMusic;
			Title = "Apple Music";
			MinimalLicenseForTransferFrom = Shared.Licensing.Enum.MusConvLicense.Ultimate;
			MinimalLicenseForTransferTo = Shared.Licensing.Enum.MusConvLicense.Ultimate;
            RegState = RegistrationState.Unlogged;
			SourceType = DataSource.AppleMusic;
			LogoKey = LogoStyleKey.AppleLogo;
			SideLogoKey = LeftSideBarLogoKey.AppleSideLogo;
			CurrentVMType = VmType.Service;
			Model = new AppleModel();
			IsHelpVisible = true;
			IsReplaceAvailable = false;
			BaseUrl = "https://music.apple.com/us/browse";
			AlbumDirectUrl = "https://music.apple.com/album/";
			SearchUrl = "https://music.apple.com/search?term=";

			NavigateHelpCommand = ReactiveCommand
				.Create(() => m.NavigateTo(NavigationKeysChild.AppleMusicHelp));

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnAppleMusicCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnAppleMusicCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new AddTracksCommand(Command_AddTrack, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnAppleMusicCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			var commandRelatedTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnAppleMusicCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandFeaturedTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
			};

            var commandBarCreateSmartPlaylistCommand = new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.CommandBar);
            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
				new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
				new SplitCommand(Command_Split, CommandTaskType.CommandBar),
				new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.DropDownMenu),
				new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.DropDownMenu),
				new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.CommandBar),
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
				new SortCommand(Command_Sort, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
				commandBarCreateSmartPlaylistCommand,
				new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.DropDownMenu),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewOnAppleMusicCommand(CommandArtist_WebOpen),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};
			
			var tracksStatsCommands = new List<Command_TaskItem>
			{
				new ReloadCommand(Command_Reload, CommandTaskType.CommandBar),
				new ViewOnAppleMusicCommand(CommandTrack_Open),
			};

			var artistsStatsCommands = new List<Command_TaskItem>
			{
				new ReloadCommand(Command_Reload, CommandTaskType.CommandBar),
				new ViewOnAppleMusicCommand(CommandArtist_WebOpen)
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_SearchWithAlbums, Transfer_Send, m, true)
			};
			var albumTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
			var artistTransfer = new List<TaskBase_TaskItem>
			{
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send, m, true)
			};

			#endregion TransferTasks

			#region Tabs

			#region Stats

			_artistsStatsTab = new StatsCategoryTab("Top Artists", StatsTabIconsStyleKey.AppleTopArtists,
				new StatsContentTab[]
				{
					new StatsContentTab (m, DateTime.Now.Year.ToString(),
						new Initial_TaskItem("Reload", Initial_Update_Artists_CurrentYear),
						artistTransfer, artistsStatsCommands, LwTabStyleKey.ItemStyleArtist),
					new StatsContentTab (m, (DateTime.Now.Year - 1).ToString(),
						new Initial_TaskItem("Reload", Initial_Update_Artists_LastYear),
						artistTransfer, artistsStatsCommands, LwTabStyleKey.ItemStyleArtist),
				});

			_tracksStatsTab = new StatsCategoryTab("Top Tracks", StatsTabIconsStyleKey.AppleTopTracks,
				new StatsContentTab[]
				{
					new StatsContentTab (m, DateTime.Now.Year.ToString(),
						new Initial_TaskItem("Reload", Initial_Update_Tracks_CurrentYear),
						trackTransfer, tracksStatsCommands, LwTabStyleKey.ItemStyleTrack),
					new StatsContentTab (m, (DateTime.Now.Year - 1).ToString(),
						new Initial_TaskItem("Reload", Initial_Update_Tracks_LastYear),
						trackTransfer, tracksStatsCommands, LwTabStyleKey.ItemStyleTrack),
				});

			_albumsStatsTab = new StatsCategoryTab("Top Albums", StatsTabIconsStyleKey.AppleTopAlbums,
				new StatsContentTab[]
				{
					new StatsContentTab (m, DateTime.Now.Year.ToString(),
						new Initial_TaskItem("Reload", Initial_Update_Albums_CurrentYear),
						albumTransfer, tracksStatsCommands, LwTabStyleKey.ItemStyleAlbum),
					new StatsContentTab (m, (DateTime.Now.Year - 1).ToString(),
						new Initial_TaskItem("Reload", Initial_Update_Albums_LastYear),
						albumTransfer, tracksStatsCommands, LwTabStyleKey.ItemStyleAlbum),
				});

			_genresStatsTab = new StatsCategoryTab("Top Genres", StatsTabIconsStyleKey.AppleTopGenres,
				new StatsContentTab[]
				{
					new StatsContentTab (m, DateTime.Now.Year.ToString(),
						new Initial_TaskItem("Reload", Initial_Update_Genres_CurrentYear),
						EmptyTransfersBase, tracksStatsCommands, LwTabStyleKey.ItemStyleGenre),
					new StatsContentTab (m, (DateTime.Now.Year - 1).ToString(),
						new Initial_TaskItem("Reload", Initial_Update_Genres_LastYear),
						EmptyTransfersBase, tracksStatsCommands, LwTabStyleKey.ItemStyleGenre),
				});

			#endregion Stats

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandPlaylistTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

            var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistTransfer, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var featuredTab = new PlaylistTabViewModelBase(m, AppleMusicFeaturedPlaylists.FeaturedPlaylists, 
				LwTabIconKey.PlaylistIcon, EmptyTransfersBase, commandFeaturedTab,
				new Initial_TaskItem("Reload", Initial_Update_Featured), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var hitsTab = new AppleUltimateTabViewModel(m, AppleMusicFeaturedPlaylists.TodaysHits, 
				LwTabIconKey.TrackIcon, EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Hits), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var newMusicDailyTab = new AppleUltimateTabViewModel(m, AppleMusicFeaturedPlaylists.NewMusicDaily,
				LwTabIconKey.TrackIcon, EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_NewMusicDaily), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var countryTab = new TrackTabViewModelBase(m, AppleMusicFeaturedPlaylists.TodaysCountry,
				LwTabIconKey.TrackIcon, EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Country), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var easyHitsTab = new TrackTabViewModelBase(m, AppleMusicFeaturedPlaylists.TodaysEasyHits,
				LwTabIconKey.TrackIcon, EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_EasyHits), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var aListPopTab = new TrackTabViewModelBase(m, AppleMusicFeaturedPlaylists.AListPop,
				LwTabIconKey.TrackIcon, EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_AListPop), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var topSongsTab = new TrackTabViewModelBase(m, AppleMusicFeaturedPlaylists.TopSongs,
				LwTabIconKey.TrackIcon, EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_TopSongs), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var youMightAlsoLikeTab = new TrackTabViewModelBase(m, AppTabs.YouMightAlsoLike,
				LwTabIconKey.YouMightAlsoLikeTrackIcon, trackTransfer, commandRelatedTab,
				new Initial_TaskItem("Reload", Initial_Update_YouMightAlsoLike), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var musicVideosTab = new TrackTabViewModelBase(m, AppTabs.MusicVideos,
				LwTabIconKey.MixedForYouPlaylistIcon, EmptyTransfersBase, commandRelatedTab,
				new Initial_TaskItem("Reload", Initial_Update_MusicVideos), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#region Recently Added Tab

			var recentlyAddedPlaylistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandFeaturedTab, 
				new Initial_TaskItem("Reload", Initial_Update_RecentlyAddedPlaylists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var recentlyAddedAlbumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTab, 
				new Initial_TaskItem("Reload", Initial_Update_RecentlyAddedAlbums), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var recentlyAddedArtistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, EmptyCommandsBase, 
				new Initial_TaskItem("Reload", Initial_Update_RecentlyAddedArtists), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			recentlyAddedPlaylistsTab.ServiceImage = "avares://MusConv/Assets/Tiles/Playlists.png";
			recentlyAddedAlbumsTab.ServiceImage = "avares://MusConv/Assets/Tiles/Albums.png";
			recentlyAddedArtistsTab.ServiceImage = "avares://MusConv/Assets/Tiles/Artists.png";

			_recentlyAddedTab = new TilesTabViewModelBase(m, AppTabs.RecentlyAdded,
				LwTabStyleKey.ItemStyleCharts, LwTabIconKey.UserMonthSongsTrackIcon, new(),
				new(),
				new Initial_TaskItem("Reload", Initial_Update_RecentlyAdded),
				new TabViewModelBase[] {
					recentlyAddedPlaylistsTab,
					recentlyAddedAlbumsTab,
					recentlyAddedArtistsTab
				},
				EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion

			_statsTab = new StatsViewModelBase(m, AppTabs.Stats,
				LwTabStyleKey.ItemStyleStats, LwTabIconKey.StatsIcon,
                new[] { _artistsStatsTab, _tracksStatsTab, _albumsStatsTab, _genresStatsTab },
				EmptyTransfersBase, EmptyCommandsBase,
				new Initial_TaskItem("Reload", Initial_Update_Stats),
				tracksStatsCommands,
				new LogOut_TaskItem("LogOut", Log_Out));

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
			Tabs.Add(featuredTab);
			Tabs.Add(hitsTab);
			Tabs.Add(newMusicDailyTab);
			Tabs.Add(youMightAlsoLikeTab);
			Tabs.Add(musicVideosTab);
			Tabs.Add(_recentlyAddedTab);
			Tabs.Add(_statsTab);


			//Tabs.Add(countryTab);
			//Tabs.Add(topSongsTab);

			#endregion

            HighlightableCommands = new()
            {
                [playlistsTab] = new Command_TaskItem[] { commandBarCreateSmartPlaylistCommand }
            };

            IHighlightableCommands highlightableCommands = this;
            playlistsTab.StoppedLoading += highlightableCommands.OnTabStoppedLoading;
        }

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (IsAuthenticated())
				{
					await InitialUpdateForCurrentTab().ConfigureAwait(false);
					return true;
				}

				if (SaveLoadCreds.IsCredsExists(out var data) && await IsServiceDataExecuted(data))
					return false;

				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}

			return false;
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			LogOutRequired = true;
			SaveLoadCreds.DeleteServiceData();
			RegState = RegistrationState.Unlogged;
			AppleModel.AppleUser.Token = null;
			foreach (var item in Tabs)
			{
				item.MediaItems.Clear();
			}
			OnLogoutReceived(new AuthEventArgs(Model));
			ClearAllMediaItems();
			await Dispatcher.UIThread.InvokeAsync(NavigateToMain);
			return true;
		}

		public override bool IsAuthenticated()
		{
			return AppleModel.IsAuthenticated();
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			OnLoginPageLeft();
			SetToken(s.ToString());

			if (!AppleModel.IsAuthenticated())
				return;

			await InitialAuthorization(s.ToString()).ConfigureAwait(false);
		}

		private async Task InitialAuthorization(string token)
		{
			SaveLoadCreds.SaveData(new List<string> { token });
			RegState = RegistrationState.Logged;

			if (IsSending)
			{
				WaitAuthentication.Set();
				IsSending = false;
			}
			else
			{
				await InitialUpdateForCurrentTab();
				OnAuthReceived(new AuthEventArgs(Model));
			}

			Initial_Setup();
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			SetToken(data[Title].FirstOrDefault());

			if (await Model.IsSavedAuthDataValid())
			{
				await InitialAuthorization(data[Title].FirstOrDefault()).ConfigureAwait(false);
				return true;
			}

			if (SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.LogOutOnLogInError))
			{
				await Log_Out();
				return false;
			}

			await ShowAuthorizationError();
			return false;
		}

		#endregion AuthMethods

		#region InitialUpdate

		public override Task<bool> Initial_Update_Playlists(bool forceUpdate = false)
		{
			return Initial_Update_Playlists_DetailedLoading(forceUpdate);
		}

		public override Task<bool> Initial_Update_Featured(bool forceUpdate = false)
		{
			return Initial_Update_Featured_DetailedLoading(forceUpdate);
		}

		public override Task<bool> Initial_Update_Album(bool forceUpdate = false)
		{
			return Initial_Update_Album_DetailedLoading(forceUpdate);
		}

		private Task<bool> Initial_Update_Stats(bool forceUpdate)
		{
			return _statsTab.SelectedCategoryTab.SelectedContentTab.InitialMethod.Initial_DoWork(forceUpdate);
		}

		private Task<bool> Initial_Update_Artists_CurrentYear(bool forceUpdate)
		{
			return InitialUpdateArtistsByYear(DateTime.Now.Year, forceUpdate);
		}

		private Task<bool> Initial_Update_Artists_LastYear(bool forceUpdate)
		{
			return InitialUpdateArtistsByYear(DateTime.Now.Year - 1, forceUpdate);
		}

		private Task<bool> Initial_Update_Tracks_CurrentYear(bool forceUpdate)
		{
			return InitialUpdateTracksByYear(DateTime.Now.Year, forceUpdate);
		}

		private Task<bool> Initial_Update_Tracks_LastYear(bool forceUpdate)
		{
			return InitialUpdateTracksByYear(DateTime.Now.Year - 1, forceUpdate);
		}

		private Task<bool> Initial_Update_Albums_CurrentYear(bool forceUpdate)
		{
			return InitialUpdateAlbumsByYear(DateTime.Now.Year, forceUpdate);
		}

		private Task<bool> Initial_Update_Albums_LastYear(bool forceUpdate)
		{
			return InitialUpdateAlbumsByYear(DateTime.Now.Year - 1, forceUpdate);
		}

		private Task<bool> Initial_Update_Genres_CurrentYear(bool forceUpdate)
		{
			return InitialUpdateGenresByYear(DateTime.Now.Year, forceUpdate);
		}

		private Task<bool> Initial_Update_Genres_LastYear(bool forceUpdate)
		{
			return InitialUpdateGenresByYear(DateTime.Now.Year - 1, forceUpdate);
		}

		private Task<bool> InitialUpdateArtistsByYear(int year, bool forceUpdate)
		{
			return InitialUpdateByYear(year, AppleModel.GetTopArtists, _artistsStatsTab, forceUpdate);
		}

		private Task<bool> InitialUpdateTracksByYear(int year, bool forceUpdate)
		{
			return InitialUpdateByYear(year, AppleModel.GetTopSongs, _tracksStatsTab, forceUpdate);
		}

		private Task<bool> InitialUpdateAlbumsByYear(int year, bool forceUpdate)
		{
			return InitialUpdateByYear(year, AppleModel.GetTopAlbums, _albumsStatsTab, forceUpdate);
		}

		private Task<bool> InitialUpdateGenresByYear(int year, bool forceUpdate)
		{
			return InitialUpdateByYear(year, AppleModel.GetTopGenres, _genresStatsTab, forceUpdate);
		}

		private Task<bool> InitialUpdateByYear<T>(int year, Func<int, Task<List<T>>> getMethod,
			StatsCategoryTab statsTab, bool forceUpdate) where T : MusConvItemBase
		{
			return InitialUpdateBuilder(() => getMethod.Invoke(year), statsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_RecentlyAdded(bool forceUpdate)
		{
			return Task.FromResult(true);
		}

		private Task<bool> Initial_Update_RecentlyAddedPlaylists(bool forceUpdate)
		{
			return InitialUpdateBuilder(AppleModel.GetRecentlyAddedPlaylists, _recentlyAddedTab.SelectedCategoryTab, forceUpdate);
		}

		private Task<bool> Initial_Update_RecentlyAddedAlbums(bool forceUpdate)
		{
			return InitialUpdateBuilder(AppleModel.GetRecentlyAddedAlbums, _recentlyAddedTab.SelectedCategoryTab, forceUpdate);
		}

		private Task<bool> Initial_Update_RecentlyAddedArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(AppleModel.GetRecentlyAddedArtists, _recentlyAddedTab.SelectedCategoryTab, forceUpdate);
		}

		private Task<bool> Initial_Update_MusicVideos(bool forceUpdate)
		{
			return InitialUpdateBuilder(AppleModel.GetMusicVideos, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_AListPop(bool forceUpdate)
		{
			return InitialUpdateByPlaylistName(AppleMusicFeaturedPlaylists.AListPop, forceUpdate);
		}

		private Task<bool> Initial_Update_Country(bool forceUpdate)
		{
			return InitialUpdateByPlaylistName(AppleMusicFeaturedPlaylists.TodaysCountry, forceUpdate);
		}

		private Task<bool> Initial_Update_EasyHits(bool forceUpdate)
		{
			return InitialUpdateByPlaylistName(AppleMusicFeaturedPlaylists.TodaysEasyHits, forceUpdate);
		}

		private Task<bool> Initial_Update_Hits(bool forceUpdate)
		{
			return InitialUpdateByPlaylistName(AppleMusicFeaturedPlaylists.TodaysHits, forceUpdate);
		}

		private Task<bool> Initial_Update_NewMusicDaily(bool forceUpdate)
		{
			return InitialUpdateByPlaylistName(AppleMusicFeaturedPlaylists.NewMusicDaily, forceUpdate);
		}

		private Task<bool> Initial_Update_TopSongs(bool forceUpdate)
		{
			return InitialUpdateByPlaylistName(AppleMusicFeaturedPlaylists.TopSongs, forceUpdate);
		}

		private Task<bool> InitialUpdateByPlaylistName(string name, bool forceUpdate)
		{
			return InitialUpdateBuilder(() => GetPlaylistTracksByName(name), SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region Commands

		public override Task Command_OpenTracksPage(object arg)
		{
			var playlist = arg as MusConvPlayList ?? (arg as List<MusConvModelBase>)?.FirstOrDefault() as MusConvPlayList;
			if (playlist is null)
				return Task.CompletedTask;

			if (!playlist.IsInteractable)
				return ShowLicenseMessageForPlaylist(playlist.Id);
			
			return base.Command_OpenTracksPage(arg);
		}

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			return ShowHelp(MessageBoxText.AppleMusicHelp);
		}

		#endregion Commands

		#region OpeningMethods

		public override Task CommandTrack_Open(object arg)
		{
			if ((arg as List<MusConvModelBase>)?.FirstOrDefault() is not MusConvTrack track)
				return Task.CompletedTask;

			return SearchTrackAndOpenUrl(track);
		}

		public static Task CommandTrack_WebOpen(object arg)
		{
			if (arg is not MusConvTrack track)
				return Task.CompletedTask;

			return SearchTrackAndOpenUrl(track);
		}

		public static async Task CommandArtist_WebOpen(object arg)
		{
			if ((arg as List<MusConvModelBase>)?.FirstOrDefault() is not MusConvArtist artist)
				return;

			try
			{
				var searchResult = await AppleUser.SearchArtist(artist.Title);
				var artistsData = searchResult?.Results?.Artists?.Data;

				if (artistsData is not null)
				{
					OpenUrlExtension.OpenUrl(artistsData.FirstOrDefault()?.Attributes?.Url);
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}
		}

		private static async Task SearchTrackAndOpenUrl(MusConvTrack track)
		{
			try
			{
				var sCriteria = $"{track.Title} {track.Artist}";
				var searchResult = await AppleUser.SearchTrack(sCriteria);
				var tracksData = searchResult?.Results?.Songs?.Data;

				if (tracksData is not null)
				{
					OpenUrlExtension.OpenUrl(tracksData.FirstOrDefault()?.Attributes?.Url);
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}
		}

		#endregion OpeningMethods

		#region SortMethods

		public override async Task ConfirmSortClick(object obj)
		{
			SelectedTab.Loading = true;
			SelectedTab.LoadingText = MusConvConfig.TracksSorting;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			try
			{
				await Model.RemovePlaylist(TargetPlaylist);
				var createModel = new MusConvPlaylistCreationRequestModel(TargetPlaylist);
				var playlist = await Model.CreatePlaylist(createModel);
				await Model.AddTracksToPlaylist(playlist, TargetPlaylist.AllTracks);
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
			}

			await MainViewModel.SelectedItem.SelectedTab.InitialMethod.Initial_DoWork(true);
			SelectedTab.Loading = false;
		}

		#endregion SortMethods

		#region TransferMethods

		/// <summary>
		/// Dictionary key - playlist title, value - track after search
		/// </summary>
		/// <param name="result"></param>
		/// <param name="index"></param>
		/// <param name="progressReport"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result,
			int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			using var manager = QueueProvider.CreateTransferQueueManager();
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);

			foreach (var resultKey in result.Keys)
			{
				try
				{
					var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
					var playlist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);
					if (playlist is not null)
					{
						MainViewModel.ResultVM.MediaItemIds.Add(playlist.Id);
						//apple music add tracks improperly when there are duplicates in the same request
						//so we need to add set of tracks first, after that add duplicates
						var allTracks = result[resultKey].Where(t => t.ResultItems?.Count > 0)
							.Select(x => x.ResultItems.FirstOrDefault());
						var distinctTracks = allTracks.DistinctBy(x => new { x.Id }).ToList();
						//adding set of tracks, work properly now
						foreach (var tracks in distinctTracks.SplitList())
						{
							token.ThrowIfCancellationRequested();
							try
							{
								await Model.AddTracksToPlaylist(playlist, tracks).ConfigureAwait(false);
							}
							catch (Exception e)
							{
								MusConvLogger.LogFiles(e);
							}

							progressReport.Report(new ReportCount(tracks.Count,
								$"Adding \"{tracks.FirstOrDefault()?.Title}\" to playlist \"{resultKey}\"",
								ReportType.Sending));
						}
					}
					else
					{
						MusConvLogger.LogFiles(new Exception("Incorrect response"));
					}
				}
				catch (AppleUserSubscriptionException ex)
				{
					// The error show when user doesn't have any subscription to Apple Music

					MusConvLogger.LogFiles(SentryEventBuilder.Build(ex).WithVisibleTag(true),
						AppleExceptionMessages.FailedCreateAppleMusicPlaylistBecauseDoesNotHavePaidSubscription);
					await ShowError(AppleExceptionMessages
						.FailedCreateAppleMusicPlaylistBecauseDoesNotHavePaidSubscription);
				}
				catch (Exception e)
				{
					MusConvLogger.LogFiles(e);
				}
			}

			progressReport.Report(GetPlaylistsReportCount(result));

			//TODO: fix adding duplicates to playlist
		}

		public override async Task Transfer_SaveInTo(object[] items)
		{
            MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!IsAuthenticated())
			{
				if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) ||
					!await IsServiceDataExecuted(data))
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				}
				else
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				}
			}
			else
			{
				WaitAuthentication.Set();
				IsSending = false;
			}

			await Transfer_DoWork(items[0]);
		}

		#endregion TransferMethods

		#region InnerMethods

		private async Task<IEnumerable<MusConvTrack>> GetPlaylistTracksByName(string name)
		{
			var playlist = await Model.GetPlaylistByName(name);
			return playlist.AllTracks;
		}

		private void SetToken(string token)
		{
			AppleModel.AppleUser.Token = new AppleMusicUserToken { MusicUserToken = token };
			AppleModel.IsAuthorized = true;
		}

		public async Task ShowLicenseMessageForPlaylist(string playlistId)
		{
			var playlist = await AppleModel.GetPlaylistDetails(playlistId);
			var message = Texts.UpgradeToUltimateToAccessPlaylist(playlist.Title, playlist.Description);
			await SelectedTab.ShowUpgradeToUltimateLicenseMessage(message);
		}

		#endregion InnerMethods
	}
	
	internal class AppleUltimateTabViewModel : TrackTabViewModelBase
	{
		public AppleUltimateTabViewModel(MainViewModelBase mainViewModel, string title, LwTabIconKey lwTabIcon,
			List<TaskBase_TaskItem> tti, List<Command_TaskItem> cti, Initial_TaskItem iti,
			List<Command_TaskItem> ctt = null, LogOut_TaskItem logOut = null, string watermark = null)
			: base(mainViewModel, title, lwTabIcon, tti, cti, iti, ctt, logOut, watermark)
		{
		}

		public AppleUltimateTabViewModel(MainViewModelBase mainViewModel, LwTabIconKey lwTabIcon,
			List<TaskBase_TaskItem> tti, List<Command_TaskItem> cti, Initial_TaskItem iti,
			List<Command_TaskItem> ctt = null, LogOut_TaskItem logOut = null, string watermark = null)
			: base(mainViewModel, lwTabIcon, tti, cti, iti, ctt, logOut, watermark)
		{
		}
		
		public override async Task<bool> CanOpen()
		{
			if (MusConvLogin.IsFullLicense)
				return true;
			
			var appleVM = MainViewModel.SelectedItem as AppleMusicViewModel;
			var playlist = await appleVM.AppleModel.GetPlaylistByName(Title);
			await appleVM.ShowLicenseMessageForPlaylist(playlist.Id);
			
			return false;
		}
	}
	
}