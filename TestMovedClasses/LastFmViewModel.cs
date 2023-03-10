using Avalonia.Threading;
using IF.Lastfm.Core.Api.Enums;
using IF.Lastfm.Core.Objects;
using MusConv.Abstractions;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class LastFmViewModel : SectionViewModelBase
	{
		#region Fields

		public bool AnyTabWasClicked { get; set; }
        private readonly StatsViewModelBase _statsTab;
        private readonly StatsCategoryTab _artistsStatsTab;
        private readonly StatsCategoryTab _tracksStatsTab;
        private readonly StatsCategoryTab _albumsStatsTab;
        private readonly TilesTabViewModelBase _bookmarkTab;
		private LastfmModel LastfmModel => Model as LastfmModel;

		#endregion Fields

		#region Constructors

		public LastFmViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Last.fm";
			RegState = RegistrationState.Unlogged;
			LoginPassPageFirstField = "Username";
			SourceType = DataSource.Lastfm;
			IsReplaceAvailable = false;
			LogoKey = LogoStyleKey.LastfmLogo;
			SideLogoKey = LeftSideBarLogoKey.LastfmSideLogo;
			CurrentVMType = VmType.Service;
			Model = new LastfmModel();
			BaseUrl = "https://www.last.fm/";
			SearchUrl = "https://www.last.fm/search?q=";

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnLastFmCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewOnLastFmCommand(CommandTrack_Open),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnLastFmCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandStatsTab = new List<Command_TaskItem>
			{
				new ReloadCommand(Command_Reload, CommandTaskType.CommandBar),
				new ViewOnLastFmCommand(CommandTrack_Open),
			};

			#endregion Commands

			#region TransferTasks

			var transferTracks = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m)
			};
			var transferPlaylist = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

            var playlistsTab = new PlaylistTabViewModelBase(m, AppTabs.Playlists, LwTabIconKey.PlaylistIcon, transferPlaylist,
	            commandPlaylistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));
            
            var favoriteTracksTab = new TrackTabViewModelBase(m, AppTabs.FavoriteTracks, LwTabIconKey.TrackIcon, transferTracks,
	            commandTracksTab,
                new Initial_TaskItem("Reload", Initial_Update_Tracks), new(),
                new LogOut_TaskItem("LogOut", Log_Out));
            
            var recentScrobblesTracksTab = new TrackTabViewModelBase(m, "Scrobbles", LwTabIconKey.TrackIcon, EmptyTransfersBase,
	            commandTracksTab,
                new Initial_TaskItem("Reload", Initial_Update_Recent_Scrobbles_Tracks), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));
            
            var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, EmptyTransfersBase,
                commandArtistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Artists), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

			#region Stats

			_artistsStatsTab = new StatsCategoryTab("Top Artists", StatsTabIconsStyleKey.AppleTopArtists,
				new StatsContentTab[]
				{
					new(m, "All Time",
						new Initial_TaskItem("Reload", Initial_Update_AllTimeArtists),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleArtist),
					new(m, "Year",
						new Initial_TaskItem("Reload", Initial_Update_YearArtists),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleArtist),
					new(m, "6 months",
						new Initial_TaskItem("Reload", Initial_Update_6MonthsArtists),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleArtist),
					new(m, "3 months",
						new Initial_TaskItem("Reload", Initial_Update_3MonthsArtists),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleArtist),
					new(m, "Month",
						new Initial_TaskItem("Reload", Initial_Update_MonthArtists),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleArtist),
					new(m, "Week",
						new Initial_TaskItem("Reload", Initial_Update_WeekArtists),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleArtist),
				});
			_tracksStatsTab = new StatsCategoryTab("Top Tracks", StatsTabIconsStyleKey.AppleTopTracks,
				new StatsContentTab[]
				{
					new(m, "All Time",
						new Initial_TaskItem("Reload", Initial_Update_AllTimeTracks),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleTrack),
					new(m, "Year",
						new Initial_TaskItem("Reload", Initial_Update_YearTracks),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleTrack),
					new(m, "6 months",
						new Initial_TaskItem("Reload", Initial_Update_6MonthsTracks),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleTrack),
					new(m, "3 months",
						new Initial_TaskItem("Reload", Initial_Update_3MonthsTracks),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleTrack),
					new(m, "Month",
						new Initial_TaskItem("Reload", Initial_Update_MonthTracks),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleTrack),
					new(m, "Week",
						new Initial_TaskItem("Reload", Initial_Update_WeekTracks),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleTrack),
				});
			_albumsStatsTab = new StatsCategoryTab("Top Genres", StatsTabIconsStyleKey.AppleTopAlbums,
				new StatsContentTab[]
				{
					new(m, "All Time",
						new Initial_TaskItem("Reload", Initial_Update_AllTimeAlbums),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleAlbum),
					new(m, "Year",
						new Initial_TaskItem("Reload", Initial_Update_YearAlbums),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleAlbum),
					new(m, "6 months",
						new Initial_TaskItem("Reload", Initial_Update_6MonthsAlbums),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleAlbum),
					new(m, "3 months",
						new Initial_TaskItem("Reload", Initial_Update_3MonthsAlbums),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleAlbum),
					new(m, "Month",
						new Initial_TaskItem("Reload", Initial_Update_MonthAlbums),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleAlbum),
					new(m, "Week",
						new Initial_TaskItem("Reload", Initial_Update_WeekAlbums),
						EmptyTransfersBase, commandStatsTab, LwTabStyleKey.ItemStyleAlbum),
				});

			#endregion Stats

            #region Bookmark Tab

            var bookmarkTracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, EmptyTransfersBase,
	            EmptyCommandsBase, new Initial_TaskItem("Reload", Initial_Update_BookmarkTracks), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            var bookmarkAlbumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, EmptyTransfersBase,
	            EmptyCommandsBase, new Initial_TaskItem("Reload", Initial_Update_BookmarkAlbums), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var bookmarkArtistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, EmptyTransfersBase,
                commandArtistsTab, new Initial_TaskItem("Reload", Initial_Update_BookmarkArtists), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            bookmarkTracksTab.ServiceImage = "avares://MusConv/Assets/Tiles/Playlists.png";
            bookmarkAlbumsTab.ServiceImage = "avares://MusConv/Assets/Tiles/Albums.png";
            bookmarkArtistsTab.ServiceImage = "avares://MusConv/Assets/Tiles/Artists.png";

            _bookmarkTab = new TilesTabViewModelBase(m, "Bookmarks",
                LwTabStyleKey.ItemStyleCharts, LwTabIconKey.UserMonthSongsTrackIcon, EmptyTransfersBase,
                EmptyCommandsBase,
                new Initial_TaskItem("Reload", () => {}),
                new TabViewModelBase[] {
                    bookmarkTracksTab,
                    bookmarkAlbumsTab,
                    bookmarkArtistsTab
                },
                new(),
                new LogOut_TaskItem("LogOut", Log_Out));

            #endregion

			_statsTab = new StatsViewModelBase(m, AppTabs.Stats,
				LwTabStyleKey.ItemStyleStats, LwTabIconKey.StatsIcon,
				new[] { _artistsStatsTab, _tracksStatsTab, _albumsStatsTab },
				EmptyTransfersBase,
				new List<Command_TaskItem>(),
				new Initial_TaskItem("Reload", Initial_Update_Stats),
				commandStatsTab,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(favoriteTracksTab);
			Tabs.Add(recentScrobblesTracksTab);
			Tabs.Add(artistsTab);
            Tabs.Add(_bookmarkTab);
			Tabs.Add(_statsTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (!Model.IsAuthorized)
				{
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
					{
						return false;
					}

					Url = await LastfmModel.GetAuthUrlAsync();
					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
					return false;
				}
				else
				{
					await InitialUpdateForCurrentTab().ConfigureAwait(false);
					return true;
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			var creds = s as LastFmCreds;

			var username = await Model.GetUserIdentifier();
			var key = LastfmModel.Api.Auth.UserSession.Token;

			OnLoginPageLeft();

			var session = new LastUserSession()
			{
				IsSubscriber = true,
				Token = key,
				Username = username,
				Cookies = creds.CookieString,
				CsrfToken = creds.CsrfToken
			};

			LastfmModel.LoadSession(session);
			UserEmail = username;

			if (!Model.IsAuthorized)
			{
				await ShowMessage("Authorization failed", Icon.Error);
				return;
			}

			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			await InitialAuthorization();

			SaveLoadCreds.SaveData(new List<string> { Serializer.Serialize(session) });
		}

		public override async Task InitialAuthorization()
		{
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
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			UserEmail = string.Empty;
			Url = await LastfmModel.GetAuthUrlAsync();
			OnLogoutReceived(new AuthEventArgs(Model));

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				ClearAllMediaItems();
				NavigateToBrowserLoginPage();
			});

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var creds = Serializer.Deserialize<LastUserSession>(data[Title].FirstOrDefault());

			LastfmModel.LoadSession(creds);

			UserEmail = await Model.GetUserIdentifier();

			await InitialAuthorization();
			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

		public override Task<bool> Initial_Update_Playlists(bool forceUpdate = false)
		{
			return Initial_Update_Playlists_DetailedLoading(forceUpdate);
		}

		public override Task<bool> Initial_Update_Tracks(bool forceUpdate = false)
		{
			AnyTabWasClicked = true;
			return InitialUpdateBuilder(Model.GetFavorites, SelectedTab, forceUpdate);
		}

		public override Task<bool> Initial_Update_Artists(bool forceUpdate = false)
		{
			AnyTabWasClicked = true;
			return InitialUpdateBuilder(Model.GetArtists, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_Stats(bool forceUpdate)
		{
			return _statsTab.SelectedCategoryTab.SelectedContentTab.InitialMethod.Initial_DoWork(forceUpdate);
		}

		private Task<bool> Initial_Update_AllTimeTracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopTracks(LastStatsTimeSpanHtml.Overall), _tracksStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_YearTracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopTracks(LastStatsTimeSpanHtml.Year), _tracksStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_6MonthsTracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopTracks(LastStatsTimeSpanHtml.Half), _tracksStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_3MonthsTracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopTracks(LastStatsTimeSpanHtml.Quarter), _tracksStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_MonthTracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopTracks(LastStatsTimeSpanHtml.Month), _tracksStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_WeekTracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopTracks(LastStatsTimeSpanHtml.Week), _tracksStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_AllTimeAlbums(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopAlbums(LastStatsTimeSpan.Overall), _albumsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_YearAlbums(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopAlbums(LastStatsTimeSpan.Year), _albumsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_6MonthsAlbums(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopAlbums(LastStatsTimeSpan.Half), _albumsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_3MonthsAlbums(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopAlbums(LastStatsTimeSpan.Quarter), _albumsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_MonthAlbums(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopAlbums(LastStatsTimeSpan.Month), _albumsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_WeekAlbums(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopAlbums(LastStatsTimeSpan.Week), _albumsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_AllTimeArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopArtists(LastStatsTimeSpanHtml.Overall), _artistsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_YearArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopArtists(LastStatsTimeSpanHtml.Year), _artistsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_6MonthsArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopArtists(LastStatsTimeSpanHtml.Half), _artistsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_3MonthsArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopArtists(LastStatsTimeSpanHtml.Quarter), _artistsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_MonthArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopArtists(LastStatsTimeSpanHtml.Month), _artistsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_WeekArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => LastfmModel.GetTopArtists(LastStatsTimeSpanHtml.Week), _artistsStatsTab.SelectedContentTab, forceUpdate);
		}

        private Task<bool> Initial_Update_BookmarkTracks(bool forceUpdate)
            => InitialUpdateBuilder(LastfmModel.GetBookmarkTracks, _bookmarkTab.SelectedCategoryTab, forceUpdate);

        private Task<bool> Initial_Update_BookmarkAlbums(bool forceUpdate)
            => InitialUpdateBuilder(LastfmModel.GetBookmarkAlbums, _bookmarkTab.SelectedCategoryTab, forceUpdate);

        private Task<bool> Initial_Update_BookmarkArtists(bool forceUpdate)
            => InitialUpdateBuilder(LastfmModel.GetBookmarkArtists, _bookmarkTab.SelectedCategoryTab, forceUpdate);

		private Task<bool> Initial_Update_Recent_Scrobbles_Tracks(bool forceUpdate = false)
		{
			AnyTabWasClicked = true;
			return InitialUpdateBuilder(LastfmModel.GetRecentScrobblesTracks, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region TransferMethods

		public override async Task Transfer_SaveInTo(object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthorized)
			{
				if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) || !await IsServiceDataExecuted(data))
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
				}
			}
			else
			{
				WaitAuthentication.Set();
				IsSending = false;
			}

			await Transfer_DoWork(items[0]);
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);
			var indexor = 0;
			try
			{
				foreach (var resultKey in result.Keys)
				{
					var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
					var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);
					MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

					var tracks = result[resultKey].Where(t => t?.ResultItems?.Count > 0)
										.Select(x => x.ResultItems?.FirstOrDefault()).ToList();

					token.ThrowIfCancellationRequested();
					await Model.AddTracksToPlaylist(createdPlaylist, tracks).ConfigureAwait(false);

					await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(new ReportCount(tracks.Count,
					$"Adding \"{result[resultKey][indexor++].OriginalSearchItem.Title}\" to playlist \"{resultKey}\" ",
					ReportType.Sending)));

					indexor = 0;
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}

			await Dispatcher.UIThread.InvokeAsync(() =>
				progressReport.Report(GetPlaylistsReportCount(result)));
		}

		#endregion TransferMethods
	}
}