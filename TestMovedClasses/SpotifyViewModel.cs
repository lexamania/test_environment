using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Abstractions.Extensions;
using MusConv.MessageBoxManager.Texts;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Sentry.Extensions;
using MusConv.Shared.Licensing.Enum;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Api;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Interfaces;
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
using ReactiveUI;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class SpotifyViewModel : ShareableServiceViewModelBase, IHighlightableCommands
	{
		#region Fields

		private const string _clientId = "0bafb40182a34aab9e1009362d77b7ad";
		private const string _secretId = "803c3cfae9cd47dda5882e0e17d064f2";
		private int _chosenPort;
		private readonly int[] _ports = new[] { 22333, 1410, 24445, 34333 };
		private static AuthorizationCodeAuth codeAuth;
		private readonly StatsViewModelBase _statsTab;
		private readonly StatsCategoryTab _artistsStatsTab;
		private readonly StatsCategoryTab _tracksStatsTab;
		private readonly StatsCategoryTab _genresStatsTab;
		private readonly List<Command_TaskItem> commandStaticPlaylist;
		private readonly List<Command_TaskItem> commandTracks;
		public SpotifyModel SpotifyModel => Model as SpotifyModel;
		public Dictionary<TabViewModelBase, Command_TaskItem[]> HighlightableCommands { get; }

		#endregion Fields

		#region Constructors

		public SpotifyViewModel(MainViewModelBase m) : base(m)
		{
			Model = new SpotifyModel();
			Title = "Spotify";
			CurrentVMType = VmType.Service;
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Spotify;
			LogoKey = LogoStyleKey.SpotifyLogo;
			SmallLogoKey = LogoStyleKey.SpotifyLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.SpotifySideLogo;
			IsNeedToSaveTracks = true;
			IsHelpVisible = true;
			IsMultipleAccountsSupported = true;
			BaseUrl = "https://www.spotify.com/us/";
			ArtistDirectUrl = "https://open.spotify.com/artist/";
			AlbumDirectUrl = "https://open.spotify.com/album/";
			SearchUrl = "https://open.spotify.com/search/";
            MinimalLicenseForTransferFrom = MusConvLicense.Ultimate;
            MinimalLicenseForTransferTo = MusConvLicense.Ultimate;

            NavigateHelpCommand = ReactiveCommand.Create(() =>
				m.NavigateTo(NavigationKeysChild.SpotifyHelp));

			Command_ChangeAccount = ReactiveCommand.CreateFromTask<MusicServiceBase>(ChangeAccount);

			Command_ConfirmSelectAccount = ReactiveCommand.CreateFromTask(ConfirmSelectAccount);

			#region Commands
			
			var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnSpotifyCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new AddTracksCommand(Command_AddTrack, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnSpotifyCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandBarCreateSmartPlaylistCommand = new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.CommandBar);
			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ViewOnSpotifyCommand(CommandTrack_Open),
				new EditCommand(Command_Edit, CommandTaskType.DropDownMenu),
				new EditCommand(Command_Edit, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
				new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
				new SplitCommand(Command_Split, CommandTaskType.CommandBar),
				new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
				new MergePlaylistsCommand(Command_Merge, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.DropDownMenu),
				new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.DropDownMenu),
				new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.CommandBar),
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
				new SharePlaylistCommand(Command_Playlist_Share, CommandTaskType.CommandBar),
				new SharePlaylistCommand(Command_Playlist_Share, CommandTaskType.DropDownMenu),
				new SortCommand(Command_Sort, CommandTaskType.CommandBar),
                new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
                new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
				commandBarCreateSmartPlaylistCommand,
				new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.DropDownMenu),
				/* new Command_TaskItem(Commands.Shuffle, Command_Shuffle, CommandTaskType.DropDownMenu,"")*/
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnSpotifyCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			var commandRelatedTab = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnSpotifyCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewOnSpotifyCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ViewOnSpotifyCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			commandStaticPlaylist = new List<Command_TaskItem>
			{
				new ViewOnSpotifyCommand(CommandTrack_Open),
				new ReloadPlaylistsCommand(Command_Reload, CommandTaskType.CommandBar),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new LyricsCommand(CommandLyrics_Open),
				new HelpCommand(Command_Help, CommandTaskType.CommandBar),
			};
			
			var statsCommands = new List<Command_TaskItem>
			{
				new ReloadCommand(Command_Reload, CommandTaskType.CommandBar),
				new ViewOnSpotifyCommand(CommandTrack_Open)
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_SearchWithAlbums, Transfer_Send, m, isQuickSearch: true)
			};
			var albumTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send, m)
			};
			var artistTransfer = new List<TaskBase_TaskItem>
			{
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};

			#endregion TransferTasks

			#region Stats

			_artistsStatsTab = new StatsCategoryTab("Top Artists", StatsTabIconsStyleKey.SpotifyTopArtists,
				new StatsContentTab[]
				{
					new StatsContentTab (m, "All Time",
						new Initial_TaskItem("Reload", Initial_Update_AllTimeArtists),
						artistTransfer, statsCommands, LwTabStyleKey.ItemStyleArtist),
					new StatsContentTab (m, "6 months",
						new Initial_TaskItem("Reload", Initial_Update_SixMonthsArtists),
						artistTransfer, statsCommands, LwTabStyleKey.ItemStyleArtist),
					new StatsContentTab (m, "30 days",
						new Initial_TaskItem("Reload", Initial_Update_OneMonthArtists),
						artistTransfer, statsCommands, LwTabStyleKey.ItemStyleArtist),
				});

			var pieTab = new PlotTabViewModelBase("Pie", StatsTabIconsStyleKey.Pie,
				new StatsPlotContentTab[]
				{
					new StatsPlotContentTab (m, "Pie",
						new Initial_TaskItem("Reload", Initial_Update_SpotifyPie),
						new(), new(), LwTabStyleKey.ItemStylePiePlot),
				});

			var icebergTab = new PlotTabViewModelBase("Iceberg", StatsTabIconsStyleKey.Iceberg,
				 new StatsPlotContentTab[]
				{
					new StatsPlotContentTab (m, "Iceberg",
						new Initial_TaskItem("Reload", Initial_Update_IceBerg),
						new(), new(), LwTabStyleKey.ItemStyleBarPlot),
				});
		
			_tracksStatsTab = new StatsCategoryTab("Top Tracks", StatsTabIconsStyleKey.SpotifyTopTracks,
				new StatsContentTab[]
				{
					new StatsContentTab (m, "All Time",
						new Initial_TaskItem("Reload", Initial_Update_AllTimeTracks),
						trackTransfer, statsCommands, LwTabStyleKey.ItemStyleTrack),
					new StatsContentTab (m, "6 months",
						new Initial_TaskItem("Reload", Initial_Update_SixMonthsTracks),
						trackTransfer, statsCommands, LwTabStyleKey.ItemStyleTrack),
					new StatsContentTab (m, "30 days",
						new Initial_TaskItem("Reload", Initial_Update_OneMonthTracks),
						trackTransfer, statsCommands, LwTabStyleKey.ItemStyleTrack),
				});

			_genresStatsTab = new StatsCategoryTab("Top Genres", StatsTabIconsStyleKey.SpotifyTopGenres,
				new StatsContentTab[]
				{
					new StatsContentTab (m, "All Time",
						new Initial_TaskItem("Reload", Initial_Update_AllTimeGenres),
						EmptyTransfersBase, statsCommands, LwTabStyleKey.ItemStyleGenre),
					new StatsContentTab (m, "6 months",
						new Initial_TaskItem("Reload", Initial_Update_SixMonthsGenres),
						EmptyTransfersBase, statsCommands, LwTabStyleKey.ItemStyleGenre),
					new StatsContentTab (m, "30 days",
						new Initial_TaskItem("Reload", Initial_Update_OneMonthGenres),
						EmptyTransfersBase, statsCommands, LwTabStyleKey.ItemStyleGenre),
				});

			#endregion Stats

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, AppTabs.Playlists, LwTabIconKey.PlaylistIcon,
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandPlaylistTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(m, AppTabs.LikedTracks, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistTransfer, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var featuredPlaylistsTab = new PlaylistTabViewModelBase(m, TabsKey.Featured.ToString(), 
				LwTabIconKey.PlaylistIcon, EmptyTransfersBase, commandStaticPlaylist,
				new Initial_TaskItem("Reload", Initial_Update_Featured), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var discoverWeeklyTab = new TrackTabViewModelBase(m, SpotifyFeaturedPlaylists.DiscoverWeekly,
				LwTabIconKey.DiscoverWeeklyTrackIcon, EmptyTransfersBase, commandStaticPlaylist,
				new Initial_TaskItem("Reload", Initial_Update_DiscoverWeekly), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var dailyMixTab = new PlaylistTabViewModelBase(m, SpotifyFeaturedPlaylists.DailyMix,
				LwTabIconKey.PlaylistIcon, EmptyTransfersBase, commandStaticPlaylist,
				new Initial_TaskItem("Reload", Initial_Update_DailyMix), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var dailyWellnessTab = new TrackTabViewModelBase(m, SpotifyFeaturedPlaylists.DailyWellness,
				LwTabIconKey.TrackIcon, EmptyTransfersBase, commandStaticPlaylist,
				new Initial_TaskItem("Reload", Initial_Update_DailyWellness), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var onRepeatTab = new TrackTabViewModelBase(m, SpotifyFeaturedPlaylists.OnRepeat, LwTabIconKey.TrackIcon,
				EmptyTransfersBase, commandStaticPlaylist,
				new Initial_TaskItem("Reload", Initial_Update_OnRepeat), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var topSongsTab = new TrackTabViewModelBase(m, "Top Songs " + (Convert.ToInt32(DateTime.Now.Year) - 1),
				LwTabIconKey.TrackIcon, EmptyTransfersBase, commandStaticPlaylist,
				new Initial_TaskItem("Reload", Initial_Update_TopSongs), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var topMonthSongsTab = new TrackTabViewModelBase(m, "User Month Songs", LwTabIconKey.UserMonthSongsTrackIcon, 
				EmptyTransfersBase, commandStaticPlaylist,
				new Initial_TaskItem("Reload", Initial_Update_TopMonthSongs), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var youMightAlsoLikeTab = new TrackTabViewModelBase(m, AppTabs.YouMightAlsoLike,
				LwTabIconKey.YouMightAlsoLikeTrackIcon,
				trackTransfer, commandRelatedTab,
				new Initial_TaskItem("Reload", Initial_Update_YouMightAlsoLike), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			_statsTab = new StatsViewModelBase(m, AppTabs.Stats,
				LwTabStyleKey.ItemStyleStats, LwTabIconKey.StatsIcon,
				new StatsTile[] {_artistsStatsTab, _tracksStatsTab, _genresStatsTab, pieTab, icebergTab },
				EmptyTransfersBase, EmptyCommandsBase,
				new Initial_TaskItem("Reload", Initial_Update_Stats),
				EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			/*var releaseRadarTab = new TabViewModelBase(m, SpotifyFeaturedPlaylists.ReleaseRadar,
				LwTabStyleKey.ItemStyleTrack, new List<TaskBase_TaskItem>(),
				command1, new Initial_TaskItem("Reload", Initial_Update_ReleaseRadar), commandTrack,
				new LogOut_TaskItem("LogOut", Log_Out));*/

			var spotifyWrappedTab = new SpotifyTilesTabViewModel(m, "Spotify Wrapped",
				LwTabStyleKey.ItemStyleCharts, LwTabIconKey.MixedForYouPlaylistIcon, EmptyTransfersBase,
				EmptyCommandsBase,
				new Initial_TaskItem("Reload", Initial_Update_SpotifyWrapped),
				EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
			Tabs.Add(featuredPlaylistsTab);
			Tabs.Add(discoverWeeklyTab);
			Tabs.Add(topMonthSongsTab);
			Tabs.Add(youMightAlsoLikeTab);
			Tabs.Add(spotifyWrappedTab);
			Tabs.Add(_statsTab);

			//Tabs.Add(releaseRadarTab);

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
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				IsSending = false;

				if (!Model.IsAuthenticated())
				{
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) &&
						await IsServiceDataExecuted(data))
					{
						return true;
					}

					Authentificate();
				}
				else
				{
					await InitialUpdateForCurrentTab();
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}

			return true;
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
				await InitialUpdateForCurrentTab(true).ConfigureAwait(false);
				OnAuthReceived(new AuthEventArgs(Model));
			}

			SaveLoadCreds.SaveData(Accounts.Values.Select(x => x.Creds).ToList());
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			if (Accounts.ContainsKey(Model))
				SaveLoadCreds.DeleteSingleServiceData(Accounts[Model].Creds);

			Accounts.Remove(Model);		

			if (Accounts.Count == 0)
			{
				await Model.Logout();
				Model = new SpotifyModel();
				RegState = RegistrationState.Unlogged;
				OnLogoutReceived(new AuthEventArgs(Model));
			}
			else
			{
				Model = Accounts.LastOrDefault().Key;
			}

			foreach (var item in Tabs)
			{
				item.MediaItems.Clear();
			}

			await Dispatcher.UIThread.InvokeAsync(NavigateToMain);
			return true;
		}

		public override async Task AddAccount()
		{
			await new SpotifyModel().Logout();
			//todo: more elegant solution
			//need some time to wait until logout is completed
			await Task.Delay(1000);
			Authentificate();
		}

		public override async Task AddAccountAndTransfer()
		{
			await new SpotifyModel().Logout();
			await Task.Delay(1000);
			IsNeedToAddNewAccount = true;
			await ConfirmSelectAccount();
		}

		public override async Task<bool> IsAccountAuthenticated(MusicServiceBase model, AccountInfo accountInfo)
		{
			var serviceData = Serializer.Deserialize<SpotifyWebAPI>(accountInfo.Creds);

			if (await IsModelAuthenticated(model, serviceData) && await model.IsSavedAuthDataValid())
			{
				return true;
			}

			SaveLoadCreds.DeleteSingleServiceData(accountInfo.Creds);
			Accounts.Remove(model);
			return false;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			if (Accounts.Count == 0)
			{
				await LoadUserAccountInfo(data.FirstOrDefault(x => x.Key == Title).Value).ConfigureAwait(false);
			}

			if (Accounts.Count > 0 && await IsAnyAccountAuthenticated())
			{
				await InitialAuthorization().ConfigureAwait(false);
				return true;
			}

			return false;
		}

		public override Task LoadUserAccountInfo(List<string> data)
		{
			foreach (var accountData in data)
			{
				var serviceData = Serializer.Deserialize<SpotifyWebAPI>(accountData);
				Accounts.Add(new SpotifyModel(), new AccountInfo()
				{
					AccountId = serviceData.UserId,
					Creds = accountData,
					Name = serviceData.AccountInfo
				});
			}

			return Task.CompletedTask;
		}

		public void Authentificate()
		{
			if (!IsSending)
			{
				MainViewModel.NavigateTo(NavigationKeysChild.Main);
			}

			//somewhy if we would also set Scope.UserReadBirthdate we would get
			//"Illegal scope" errror from Spotify,but seems like we dont really need this permission
			//so we could remove it without any concerns
			var scopes = Scope.UserReadPrivate | Scope.UserReadEmail | Scope.PlaylistReadPrivate |
						 Scope.UserLibraryRead | Scope.UserLibraryModify |
						 Scope.UserFollowRead | Scope.UserTopRead | Scope.UserReadPrivate |
						 Scope.Streaming | Scope.PlaylistModifyPublic |
						 Scope.PlaylistReadCollaborative | Scope.UserFollowModify |
						 Scope.UserReadRecentlyPlayed | Scope.PlaylistModifyPrivate | Scope.UserReadPlaybackState |
						 Scope.UserModifyPlaybackState | Scope.UgcImageUpload;

			if (_chosenPort == default)
			{
				foreach (var port in _ports)
				{
					if (CheckAvailableServerPort(port))
					{
						_chosenPort = port;
						break;
					}
				}
			}

			var auth = new AuthorizationCodeAuth(_clientId, _secretId, $"http://localhost:{_chosenPort}/",
				$"http://localhost:{_chosenPort}/", scopes);
			auth.AuthReceived += AuthOnAuthReceived;
			auth.Start();
			auth.OpenBrowser();
		}

		private async void AuthOnAuthReceived(object sender, AuthorizationCode payload)
		{
			if (!IsSending) NavigateToContent();

			OnLoginPageLeft();

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				MainViewModel.MinimizeWindow();
				MainViewModel.NormalizeWindow();
			});

			try
			{
				codeAuth = (AuthorizationCodeAuth)sender;
				codeAuth.Stop();
				var token = await codeAuth.ExchangeCode(payload.Code).ConfigureAwait(false);
				var api = new SpotifyWebAPI
				{
					AccessToken = token.AccessToken,
					TokenType = token.TokenType,
					Auth = codeAuth,
					RefreshToken = token.RefreshToken
				};

				var newModel = Model.IsAuthenticated() ? new SpotifyModel() : Model;
				if (!await IsModelAuthenticated(newModel, api).ConfigureAwait(false))
				{
					return;
				}

				Accounts[newModel] = new AccountInfo()
				{
					AccountId = api.UserId,
					Creds = Serializer.Serialize(api),
					Name = api.AccountInfo
				};

				if (IsSending)
					ModelTransferTo = newModel;
				else
					Model = newModel;

				await InitialAuthorization().ConfigureAwait(false);
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				await ShowError(Texts.AuthorizationFailed);
				await Dispatcher.UIThread.InvokeAsync(NavigateToMain);
			}
		}

		private async Task<bool> IsModelAuthenticated(MusicServiceBase model, SpotifyWebAPI api)
		{
			return model.IsAuthenticated() || (await model.Initialize(api)).LoginNavigationState == LoginNavigationState.Done;
		}

		#endregion AuthMethods

		#region InitialUpdate

		private Task<bool> Initial_Update_Stats(bool forceUpdate)
		{
			return _statsTab.SelectedTile.SelectedContentTab.InitialMethod.Initial_DoWork(forceUpdate);
		}

		private Task<bool> Initial_Update_SpotifyPie(bool forceUpdate = true) => Initial_Update_Plot(SpotifyModel.GetSpotifyPie);

		private Task<bool> Initial_Update_IceBerg(bool forceUpdate = true) => Initial_Update_Plot(SpotifyModel.GetArtistsIceberg);

		private async Task<bool> Initial_Update_Plot(Func<Task<MusConvPlot>> plotUpdateFunc)
		{
			var plotTab = _statsTab.SelectedTile.SelectedContentTab as StatsPlotContentTab;

			try
			{
				plotTab.Loading = true;
				plotTab.Plot = await plotUpdateFunc();
			}
			catch(Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}
			finally
			{
				plotTab.Loading = false;
			}

			return true;
		}

		private Task<bool> Initial_Update_AllTimeArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => SpotifyModel.GetTopArtistsByTerm(TimeRangeType.LongTerm), _artistsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_SixMonthsArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => SpotifyModel.GetTopArtistsByTerm(TimeRangeType.MediumTerm), _artistsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_OneMonthArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => SpotifyModel.GetTopArtistsByTerm(TimeRangeType.ShortTerm), _artistsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_AllTimeTracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => SpotifyModel.GetTopSongsByTerm(TimeRangeType.LongTerm), _tracksStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_SixMonthsTracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => SpotifyModel.GetTopSongsByTerm(TimeRangeType.MediumTerm), _tracksStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_OneMonthTracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => SpotifyModel.GetTopSongsByTerm(TimeRangeType.ShortTerm), _tracksStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_AllTimeGenres(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => SpotifyModel.GetTopGenresByTerm(TimeRangeType.LongTerm), _genresStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_SixMonthsGenres(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => SpotifyModel.GetTopGenresByTerm(TimeRangeType.MediumTerm), _genresStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_OneMonthGenres(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => SpotifyModel.GetTopGenresByTerm(TimeRangeType.ShortTerm), _genresStatsTab.SelectedContentTab, forceUpdate);
		}

		private async Task<bool> Initial_Update_SpotifyWrapped(bool forceUpdate)
		{
			var wrappedTab = SelectedTab as TilesTabViewModelBase;
			if (wrappedTab.Tabs.Count > 0 || !forceUpdate) return true;

			wrappedTab.Loading = true;

			wrappedTab.Tabs.Clear();
			Dictionary<string, IEnumerable<MusConvPlayList>> tabs;
			try
			{
				tabs = await SpotifyModel.GetSpotifyWrapped();
			}
			catch (Exception e)
			{
				HandleException(e);

				return false;
			}

			if (tabs.Count == 0)
			{
				wrappedTab.Loading = false;
				await ShowError(Texts.NoSpotifyWrappedFound);
				return false;
			}

			foreach(var tab in tabs)
			{
				try
				{
					var playlistTab = new PlaylistTabViewModelBase(MainViewModel, tab.Key, LwTabIconKey.PlaylistIcon,
						new(), commandStaticPlaylist, new(), commandTracks, new LogOut_TaskItem("LogOut", Log_Out));

					playlistTab.MediaItems.AddRange(tab.Value);
					playlistTab.ServiceImage = (playlistTab.MediaItems.First() as MusConvPlayList).ImageLink;

					wrappedTab.Tabs.Add(playlistTab);
				}
				catch (Exception e)
				{
					HandleException(e);
				}
			}

			wrappedTab.Loading = false;
			return true;

			void HandleException(Exception e)
			{
				wrappedTab.Loading = false;
				MusConvLogger.LogFiles(e);
			}
		}

		private Task<bool> Initial_Update_DiscoverWeekly(bool forceUpdate)
		{
			return InitialUpdateBuilder(SpotifyModel.GetDiscoverWeekly, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_DailyWellness(bool forceUpdate)
		{
			return InitialUpdateBuilder(SpotifyModel.GetDailyWellness, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_TopSongs(bool forceUpdate)
		{
			return InitialUpdateBuilder(SpotifyModel.GetTopSongs, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_OnRepeat(bool forceUpdate)
		{
			return InitialUpdateBuilder(SpotifyModel.GetOnRepeat, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_DailyMix(bool forceUpdate)
		{
			return InitialUpdateBuilder(SpotifyModel.GetDailyMix, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_ReleaseRadar(bool forceUpdate)
		{
			return InitialUpdateBuilder(SpotifyModel.GetReleaseRadar, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_TopMonthSongs(bool forceUpdate)
		{
			return InitialUpdateBuilder(SpotifyModel.GetTopMonthSongs, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			ShowHelp(MessageBoxText.SpotifyHelp);

			return Task.CompletedTask;
		}

		public override Task Command_ShareOnTwitter(MusConvPlayList arg)
		{
			if (!string.IsNullOrEmpty(arg.Url))
			{
				var query = "https://twitter.com/intent/tweet?text=Playlist \"\"\""
					+ $"{System.Web.HttpUtility.UrlEncode(arg.Title)}\"\"\" "
					+ $"{Texts.SharedBy}  {arg.Url}";
				OpenUrlExtension.OpenUrl(query);
			}
			else
			{
				ShowMessage(Texts.CannotSharePlaylist);
			}

			return Task.CompletedTask;
		}

		public override Task Command_ShareOnFacebook(MusConvPlayList arg)
		{
			if (!string.IsNullOrEmpty(arg.Url))
			{
				var query = $"https://www.facebook.com/sharer/sharer.php?u={arg.Url}"
					+ $"&quote=Playlist \"\"\"{arg.Title}\"\"\" {Texts.SharedBy}";
				OpenUrlExtension.OpenUrl(query);
			}
			else
			{
				ShowMessage(Texts.CannotSharePlaylist);
			}

			return Task.CompletedTask;
		}

		#endregion Commands

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			try
			{
				if (IsNeedToAddNewAccount || ModelTransferTo is null)
				{
					IsNeedToAddNewAccount = false;
					Authentificate();
				}
				else if (!await IsAccountAuthenticated(ModelTransferTo, Accounts[ModelTransferTo]))
				{
					Authentificate();
				}
				else
				{
					WaitAuthentication.Set();
					IsSending = false;
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}

			await Transfer_DoWork(items[0]).ConfigureAwait(false);
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result,
			int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);
			var covers = new Dictionary<MusConvPlayList, string>();
			var isCoverTransferRequired =
				SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.TransferPlaylistCover);
			//ToDictionary method crashes if playlists have the same title
			var hasDuplicates = (MainViewModel.TaskQItems.FirstOrDefault() as PlaylistTransfer_TaskItem)?.Items
				.Select(x => x.Title).ToList().GroupBy(n => n).Any(c => c.Count() > 1);
			if (hasDuplicates == false)
			{
				covers = result.ToDictionary(key => key.Key, value => value.Key.ImageLink);
			}

			foreach (var resultKey in result.Keys)
			{
				MusConvPlayList createdPlaylist;
				try
				{
					var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
					createdPlaylist = await ModelTransferTo.CreatePlaylist(createModel).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					MusConvLogger.LogFiles(e);
					continue;
				}

				MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

				try
				{
					if (isCoverTransferRequired && covers.ContainsKey(resultKey) && !string.IsNullOrEmpty(covers[resultKey]))
					{
						await ModelTransferTo.PlaylistImageUpdate(createdPlaylist.Id, covers[resultKey]);
					}
				}
				catch (Exception e)
				{
					MusConvLogger.LogFiles(e);
				}

				foreach (var tracks in result[resultKey].Where(t => t?.ResultItems != null && t.ResultItems.Count > 0)
							 .Select(x => x.ResultItems.First()).AllIsNotNull().ToList().SplitList())
				{
					await ModelTransferTo.AddTracksToPlaylist(createdPlaylist, tracks);
					progressReport.Report(new ReportCount(tracks.Count,
						$"Adding tracks to playlist \"{resultKey}\", please wait",
						ReportType.Sending, IsSelfTransfer));
				}

				await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(result)));
			}
		}

		#endregion TransferMethods

		#region InnerMethods

		private static bool CheckAvailableServerPort(int port)
		{
			// Evaluate current system tcp connections. This is the same information provided
			// by the netstat command line application, just in .Net strongly-typed object
			// form.  We will look through the list, and if our port we would like to use
			// in our TcpClient is occupied, we will set isAvailable to false.
			IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

			foreach (IPEndPoint endpoint in tcpConnInfoArray)
			{
				if (endpoint.Port == port)
				{
					return false;
				}
			}

			return true;
		}

		#endregion InnerMethods
	}

	internal static class SpotifyFeaturedPlaylists
	{
		public const string DiscoverWeekly = "Discover Weekly";
		public const string DailyMix = "Daily Mix";
		public const string OnRepeat = "On repeat";
		public const string DailyWellness = "Daily Wellness";
		public const string ReleaseRadar = "Release Radar";
	}

	public delegate void OnAuthCompleted();

	internal class SpotifyTilesTabViewModel : TilesTabViewModelBase
	{
		public SpotifyTilesTabViewModel
			(MainViewModelBase main, string title, LwTabStyleKey lwstyle, LwTabIconKey lwTabIcon,
			List<TaskBase_TaskItem> tti, List<Command_TaskItem> cti, Initial_TaskItem iti,
			List<Command_TaskItem> ctt = null, LogOut_TaskItem logOut = null, string watermark = null) 
			: base(main, title, lwstyle, lwTabIcon, tti, cti, iti, Array.Empty<TabViewModelBase>(), ctt, logOut, watermark)
		{
		}

		public override async Task<bool> CanOpen()
		{
			if (MusConvLogin.IsFullLicense) 
				return true;
			
			await ShowUpgradeToUltimateLicenseMessage(Texts.UpgradeToUltimateToAccessSpotifyWrapped);
			return false;
		}
	}
}