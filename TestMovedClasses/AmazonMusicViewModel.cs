using AmazonMusic.Enums;
using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Abstractions.Extensions;
using MusConv.Lib.AmazonMusic.Models.ResponseModels.Library;
using MusConv.MessageBoxManager.Texts;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Interfaces;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class AmazonMusicViewModel : SectionViewModelBase, IHighlightableCommands
	{
		#region Fields

		private AmazonModel AmazonModel => Model as AmazonModel;
		private readonly StatsViewModelBase _statsTab;
		private readonly StatsCategoryTab _artistsStatsTab;
		private readonly StatsCategoryTab _tracksStatsTab;
		private readonly StatsCategoryTab _albumsStatsTab;
        public Dictionary<TabViewModelBase, Command_TaskItem[]> HighlightableCommands { get; }

		#endregion Fields

		#region Constructors

        public AmazonMusicViewModel(MainViewModelBase mainViewModel) : base(mainViewModel)
		{
			Title = "Amazon Music";
			RegState = RegistrationState.Unlogged;
			Model = new AmazonModel(OnTokenExpired);
            MinimalLicenseForTransferFrom = Shared.Licensing.Enum.MusConvLicense.Ultimate;
            MinimalLicenseForTransferTo = Shared.Licensing.Enum.MusConvLicense.Ultimate;
			SourceType = DataSource.AmazonMusic;
			LogoKey = LogoStyleKey.AmazonLogo;
			SmallLogoKey = LogoStyleKey.AmazonLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.AmazonSideLogo;
			CurrentVMType = VmType.Service;
			Url = Urls.AmazonMusic;
			AlbumDirectUrl = "https://music.amazon.com/albums/";
			BaseUrl = "https://music.amazon.com/";
			IsHelpVisible = true;
			IsPlanVisible = true;
			NavigateHelpCommand = ReactiveCommand.Create(() =>
				mainViewModel.NavigateTo(NavigationKeysChild.AmazonHelp));

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnAmazonMusicCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnAmazonMusicCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
				new AddTracksCommand(Command_AddTrack, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnAmazonMusicCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			var commandRelatedTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnAmazonMusicCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ViewOnAmazonMusicCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar)
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewOnAmazonMusicCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar)
			};

            var commandBarCreateSmartPlaylistCommand = new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.CommandBar);
            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
				new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
				new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
				new SplitCommand(Command_Split, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),       
                new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.DropDownMenu),
				new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.DropDownMenu),
				new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.CommandBar),
				new SortCommand(Command_Sort, CommandTaskType.CommandBar),
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
                commandBarCreateSmartPlaylistCommand,
                new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.DropDownMenu)
			};

			var commandStats = new List<Command_TaskItem>
			{
				new ReloadCommand(Command_Reload, CommandTaskType.CommandBar),
				new ViewOnAmazonMusicCommand(CommandTrack_Open)
			};

			#endregion Commands

			#region TransferTasks

			var playlistsTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_SearchWithAlbums, Transfer_Send, mainViewModel, true)
			};
			var albumsTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, mainViewModel)
			};
			var tracksTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send, mainViewModel, true)
			};
			var artistsTransfer = new List<TaskBase_TaskItem>
			{
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, mainViewModel)
			};

			#endregion TransferTasks

			#region Tabs

			#region Stats

			_artistsStatsTab = new StatsCategoryTab("Top Artists", StatsTabIconsStyleKey.AppleTopArtists,
				new StatsContentTab[]
				{
					new StatsContentTab (mainViewModel, "Latest",
						new Initial_TaskItem("Reload", Initial_Update_TopArtists),
						artistsTransfer, commandStats, LwTabStyleKey.ItemStyleArtist),
				});

			_tracksStatsTab = new StatsCategoryTab("Top Tracks", StatsTabIconsStyleKey.AppleTopTracks,
				new StatsContentTab[]
				{
					new StatsContentTab (mainViewModel, "Latest",
						new Initial_TaskItem("Reload", Initial_Update_TopTracks),
						tracksTransfer, commandStats, LwTabStyleKey.ItemStyleTrack),
				});

			_albumsStatsTab = new StatsCategoryTab("Top Albums", StatsTabIconsStyleKey.AppleTopAlbums,
				new StatsContentTab[]
				{
					new StatsContentTab (mainViewModel, "Latest",
						new Initial_TaskItem("Reload", Initial_Update_TopAlbums),
						albumsTransfer, commandStats, LwTabStyleKey.ItemStyleAlbum),
				});

			#endregion Stats

			var playlistsTab = new PlaylistTabViewModelBase(mainViewModel, LwTabIconKey.PlaylistIcon,
				playlistsTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandPlaylistTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

            var tracksTab = new TrackTabViewModelBase(mainViewModel, AppTabs.AmazonLikedTracks,
				LwTabIconKey.TrackIcon, tracksTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var songHistoryTab = new TrackTabViewModelBase(mainViewModel, AppTabs.SongHistory,
				LwTabIconKey.TrackIcon, tracksTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_SongHistory), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			// var myRecentTracksTab = new TrackTabViewModelBase(mainViewModel, AppTabs.MyRecentPlays,
			// 	LwTabIconKey.TrackIcon, tracksTransfer, commandTracksTab,
			// 	new Initial_TaskItem("Reload", Initial_Update_MyRecentPlays), EmptyCommandsBase,
			// 	new LogOut_TaskItem("LogOut", Log_Out));

			// var myDiscoveryMixTab = new TrackTabViewModelBase(mainViewModel, AppTabs.MyDiscoveryMix,
			// 	LwTabIconKey.TrackIcon, tracksTransfer, commandTracksTab,
			// 	new Initial_TaskItem("Reload", Initial_Update_MyDiscoveryMix), EmptyCommandsBase,
			// 	new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(mainViewModel, LwTabIconKey.AlbumIcon,
				albumsTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistsTab = new ArtistTabViewModelBase(mainViewModel, LwTabIconKey.ArtistIcon,
				artistsTransfer, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var youMightAlsoLikeTab = new TrackTabViewModelBase(mainViewModel, AppTabs.YouMightAlsoLike,
				LwTabIconKey.YouMightAlsoLikeTrackIcon, tracksTransfer, commandRelatedTab,
				new Initial_TaskItem("Reload", Initial_Update_YouMightAlsoLike), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));			
			
			var purchasedTab = new TrackTabViewModelBase(mainViewModel, "Purchased",
				LwTabIconKey.UploadsTrackIcon, tracksTransfer, commandRelatedTab,
				new Initial_TaskItem("Reload", Initial_Update_Purchased), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			_statsTab = new StatsViewModelBase(mainViewModel, AppTabs.Stats,
				LwTabStyleKey.ItemStyleStats, LwTabIconKey.StatsIcon,
                new[] { _artistsStatsTab, _tracksStatsTab, _albumsStatsTab },
                EmptyTransfersBase, EmptyCommandsBase,
				new Initial_TaskItem("Reload", Initial_Update_Stats),
				commandStats,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
			Tabs.Add(purchasedTab);
			Tabs.Add(songHistoryTab);
			// Tabs.Add(myRecentTracksTab);
			// Tabs.Add(myDiscoveryMixTab);
			Tabs.Add(youMightAlsoLikeTab);
			Tabs.Add(_statsTab);

            HighlightableCommands = new()
            {
                [playlistsTab] = new Command_TaskItem[] { commandBarCreateSmartPlaylistCommand }
            };

            IHighlightableCommands highlightableCommands = this;
            playlistsTab.StoppedLoading += highlightableCommands.OnTabStoppedLoading;
        }

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			OnLoginPageLeft();
			await Initialize(s as AppConfigResponse, t as List<Cookie>).ConfigureAwait(false);
			await InitialAuthorizaton().ConfigureAwait(false);
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			LogOutRequired = true;
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));
			
			foreach (var item in Tabs)
				item.MediaItems.Clear();
			
			await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);

			return true;
		}

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				OnServiceSelected();

				if (Model.IsAuthenticated())
				{
					await InitialUpdateForCurrentTab().ConfigureAwait(false);
					return true;
				}

				if (SaveLoadCreds.IsCredsExists(out var data) && await IsServiceDataExecuted(data))
				{
					return false;
				}

				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				return false;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var amazonData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			var headers = Serializer.Deserialize<AppConfigResponse>(amazonData["Data"]);
			var cookie = Serializer.Deserialize<List<Cookie>>(amazonData["Cookie"])
				.Select(x => new Cookie() { Name = x.Name, Value = x.Value, Domain = x.Domain, Path = x.Path, })
				.ToList();

			if (await Initialize(headers, cookie))
			{
				await InitialAuthorizaton().ConfigureAwait(false);
				return true;
			}

			AmazonModel.Client = null;
			return false;
		}

		private async Task<bool> Initialize(AppConfigResponse headers, List<Cookie> cookies)
		{
			try
			{
				if (Model.IsAuthenticated()) return true;
				if (headers?.AccessToken == null) return false;

				var tier = System.Enum.Parse<AmazonPlan>(headers.Tier);
				GetAccountPlan(tier);

				var loginResult = await Model.Initialize(headers, cookies).ConfigureAwait(false);
				if (loginResult.LoginNavigationState != LoginNavigationState.Done)
					return false;

				await OnTokenExpired(headers, cookies);
				return true;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		private async Task InitialAuthorizaton()
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
		}

		private string GetAccountPlan(AmazonPlan tier)
		{
			Plan = AmazonModel.GetAccountPlan(tier);

			if (Plan == "Plan: Free")
			{
				ShowMessage(Texts.AmazonSubscriptionAlert);
			}

			return Plan;
		}

		#endregion AuthMethods

		#region InitialUpdate

		public override Task<bool> Initial_Update_Playlists(bool forceUpdate = false)
		{
			return Initial_Update_Playlists_DetailedLoading(forceUpdate);
		}

		public virtual Task<bool> Initial_Update_SongHistory(bool forceUpdate = false)
		{
			return InitialUpdateBuilder(AmazonModel.GetSongHistory, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_Stats(bool forceUpdate)
		{
			return _statsTab.SelectedTile.SelectedContentTab.InitialMethod.Initial_DoWork(forceUpdate);
		}

		private Task<bool> Initial_Update_TopArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => AmazonModel.GetTopArtists(), _artistsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_TopTracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => AmazonModel.GetTopTracks(), _tracksStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_TopAlbums(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => AmazonModel.GetTopAlbums(), _albumsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_Purchased(bool forceUpdate)
		{
			return InitialUpdateBuilder(AmazonModel.GetPurchased, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region Commands

		public override async Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			await ShowHelp(MessageBoxText.AmazonHelp);
		}

		#endregion Commands

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
            MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (Model.IsAuthenticated())
			{
				WaitAuthentication.Set();
				IsSending = false;
			}
			else if (SaveLoadCreds.IsCredsExists(out var data) && await IsServiceDataExecuted(data))
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			}
			else
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
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
					var creationRequestModel = new MusConvPlaylistCreationRequestModel(resultKey);
					var createdPlaylist = await Model.CreatePlaylist(creationRequestModel).ConfigureAwait(false);
					MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

					var resultLists = result[resultKey].Where(t => t?.ResultItems?.Count > 0)
						.Select(x => x.ResultItems?.FirstOrDefault()).ToList()
						.SplitList();

					foreach (var item in resultLists)
					{
						token.ThrowIfCancellationRequested();

						try
						{
							await Model.AddTracksToPlaylist(createdPlaylist, item).ConfigureAwait(false);
						}
						catch (Exception e)
						{
							MusConvLogger.LogFiles(e);
						}

						var message = $"Adding \"{result[resultKey][indexor].OriginalSearchItem.Title}\" to playlist \"{resultKey}\"";
						await Dispatcher.UIThread.InvokeAsync(() =>
							progressReport.Report(new ReportCount(item.Count, message, ReportType.Sending)));

						indexor++;
					}

					indexor = 0;
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				await ShowHelp(MessageBoxText.AmazonHelp);
			}

			await Dispatcher.UIThread.InvokeAsync(() =>
				progressReport.Report(GetPlaylistsReportCount(result)));
		}

		#endregion TransferMethods

		#region ShowError

		public new async Task ShowErrorsAsync()
		{
			if (IsResultsWindowAlreadyShowed == false && await base.ShowErrorsAsync() == true)
			{
				await ShowHelp(MessageBoxText.AmazonHelp);
			}
		}

		#endregion ShowError

		#region InnerMethods

		private Task OnTokenExpired(AppConfigResponse headers, List<Cookie> cookies)
		{
			SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(headers, cookies) });
			return Task.CompletedTask;
		}

		private static string GetSerializedServiceData(AppConfigResponse headers, List<Cookie> cookies)
		{
			return Serializer.Serialize(new Dictionary<string, string>
			{
				{ "Data", Serializer.Serialize(headers)},
				{ "Cookie", Serializer.Serialize(cookies)},
			});
		}

		#endregion InnerMethods
	}
}