using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Abstractions.Extensions;
using MusConv.Lib.Deezer;
using MusConv.MessageBoxManager.Enums;
using MusConv.MessageBoxManager.Texts;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MusConv.Abstractions.Exceptions;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class DeezerViewModel : ShareableServiceViewModelBase, IHighlightableCommands
	{
		#region Fields

		private DeezerModel DeezerModel => Model as DeezerModel;
		private readonly StatsViewModelBase _statsTab;
		private readonly StatsCategoryTab _artistsStatsTab;
		private readonly StatsCategoryTab _tracksStatsTab;
		private readonly StatsCategoryTab _albumsStatsTab;
		private readonly StatsCategoryTab _genresStatsTab;
        public Dictionary<TabViewModelBase, Command_TaskItem[]> HighlightableCommands { get; }

		#endregion Fields

		#region Constructors

        public DeezerViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Deezer";
			RegState = RegistrationState.Unlogged;
			Model = new DeezerModel();
			SourceType = DataSource.Deezer;
			LogoKey = LogoStyleKey.DeezerLogo;
			SmallLogoKey = LogoStyleKey.DeezerLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.DeezerSideLogo;
			CurrentVMType = VmType.Service;
			IsMultipleAccountsSupported = true;
			Url = Urls.Deezer;
			BaseUrl = "https://www.deezer.com/";
			ArtistDirectUrl = "https://www.deezer.com/ru/artist/";
			AlbumDirectUrl = "https://www.deezer.com/ru/album/";
			SearchUrl = "https://www.deezer.com/search/";

			Command_ChangeAccount = ReactiveCommand.CreateFromTask<MusicServiceBase>(ChangeAccount);
			IsHelpVisible = true;

			Command_ConfirmSelectAccount = ReactiveCommand.CreateFromTask(ConfirmSelectAccount);
			
			NavigateHelpCommand = ReactiveCommand
				.Create(() => m.NavigateTo(NavigationKeysChild.DeezerHelp));
			
			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnDeezerCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnDeezerCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new AddTracksCommand(Command_AddTrack, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

            var commandBarCreateSmartPlaylistCommand = new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.CommandBar);
            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ViewOnDeezerCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
				new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
				new EditCommand(Command_Edit, CommandTaskType.CommandBar),
				new SplitCommand(Command_Split, CommandTaskType.CommandBar),
				new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
                new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.DropDownMenu),
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
				new SharePlaylistCommand(Command_Playlist_Share, CommandTaskType.CommandBar),
				new SharePlaylistCommand(Command_Playlist_Share, CommandTaskType.DropDownMenu),
				new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.DropDownMenu),
				new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.CommandBar),
				new SortCommand(Command_Sort, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
                commandBarCreateSmartPlaylistCommand,
                new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.DropDownMenu),
                new EditCommand(Edit_Confirm),
			};

			var commandAddedPlaylistTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ViewOnDeezerCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new SharePlaylistCommand(Command_Playlist_Share, CommandTaskType.CommandBar),
				new SharePlaylistCommand(Command_Playlist_Share, CommandTaskType.DropDownMenu),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ViewOnDeezerCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnDeezerCommand(CommandTrack_Open),
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
				new ViewOnDeezerCommand(CommandTrack_Open),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewOnDeezerCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			var commandRecommendedTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ViewOnDeezerCommand(CommandTrack_Open),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
			};
			
			var statsCommand = new List<Command_TaskItem>
			{
				new ReloadCommand(Command_Reload, CommandTaskType.CommandBar),
				new ViewOnDeezerCommand(CommandTrack_Open)
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_SearchWithAlbums, Transfer_Send, m, true)
			};
			var albumTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m, true)
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
					new StatsContentTab (m, "Latest",
						new Initial_TaskItem("Reload", Initial_Update_TopArtists),
						artistTransfer, statsCommand, LwTabStyleKey.ItemStyleArtist),
				});

			_tracksStatsTab = new StatsCategoryTab("Top Tracks", StatsTabIconsStyleKey.AppleTopTracks,
				new StatsContentTab[]
				{
					new StatsContentTab (m, "Latest",
						new Initial_TaskItem("Reload", Initial_Update_TopTracks),
						trackTransfer, statsCommand, LwTabStyleKey.ItemStyleTrack),
				});

			_genresStatsTab = new StatsCategoryTab("Top Genres", StatsTabIconsStyleKey.AppleTopGenres,
				new StatsContentTab[]
				{
					new StatsContentTab (m, "Latest",
						new Initial_TaskItem("Reload", Initial_Update_TopGenres),
						EmptyTransfersBase, statsCommand, LwTabStyleKey.ItemStyleGenre),
				});

			_albumsStatsTab = new StatsCategoryTab("Top Albums", StatsTabIconsStyleKey.AppleTopAlbums,
				new StatsContentTab[]
				{
					new StatsContentTab (m, "Latest",
						new Initial_TaskItem("Reload", Initial_Update_TopAlbums),
						albumTransfer, statsCommand, LwTabStyleKey.ItemStyleAlbum),
				});

			#endregion Stats

			var playlistTab = new PlaylistTabViewModelBase(m, AppTabs.Playlists, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandPlaylistTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
            var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistTransfer, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));
			var featuredPlaylistsTab = new PlaylistTabViewModelBase(m, AppTabs.Recommendations, LwTabIconKey.RecommendedPlaylistIcon, 
				EmptyTransfersBase, commandRecommendedTab,
				new Initial_TaskItem("Reload", Initial_Update_Recommended_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var addedPlaylistTab = new PlaylistTabViewModelBase(m, AppTabs.AddedPlaylist, LwTabIconKey.SavedPlaylistIcon, 
				playlistTransfer, commandAddedPlaylistTab,
				new Initial_TaskItem("Reload", Initial_Update_Featured), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var youMightAlsoLikeTab = new TrackTabViewModelBase(m, AppTabs.YouMightAlsoLike, LwTabIconKey.YouMightAlsoLikeTrackIcon, 
				trackTransfer, commandRelatedTab,
				new Initial_TaskItem("Reload", Initial_Update_YouMightAlsoLike), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			_statsTab = new StatsViewModelBase(m, AppTabs.Stats,
				LwTabStyleKey.ItemStyleStats, LwTabIconKey.StatsIcon,
                new[] { _artistsStatsTab, _tracksStatsTab, _albumsStatsTab, _genresStatsTab },
				EmptyTransfersBase, EmptyCommandsBase,
				new Initial_TaskItem("Reload", Initial_Update_Stats),
				statsCommand,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs
			
			Tabs.Add(playlistTab);
			Tabs.Add(addedPlaylistTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
			Tabs.Add(featuredPlaylistsTab);
			Tabs.Add(youMightAlsoLikeTab);
			Tabs.Add(_statsTab);

            HighlightableCommands = new()
            {
                [playlistTab] = new Command_TaskItem[] { commandBarCreateSmartPlaylistCommand }
            };

            IHighlightableCommands highlightableCommands = this;
            playlistTab.StoppedLoading += highlightableCommands.OnTabStoppedLoading;
        }

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				MainViewModel.NeedLogin = this;

				if (!Model.IsAuthenticated())
				{
					if (SaveLoadCreds.IsCredsExists(out var data) && await IsServiceDataExecuted(data))
					{
						return true;
					}

					Authentificate();
				}
				else
				{
					await InitialUpdateForCurrentTab().ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				Initial_Setup();
				return false;
			}

			return true;
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			OnLoginPageLeft();

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				SelectedTab.MainViewModel.MinimizeWindow();
				SelectedTab.MainViewModel.NormalizeWindow();
			});

			if (s != null)
			{
				var newModel = Model.IsAuthenticated() ? new DeezerModel() : Model;

				var initializationResult = await newModel.Initialize(s.ToString()).ConfigureAwait(false);

				if (initializationResult.LoginNavigationState != LoginNavigationState.Done)
				{
					MusConvLogger.LogFiles(initializationResult.Error);
					
					if (initializationResult.Error is AuthorizationException authorizationException)
					{
						ShowError(authorizationException.Message);
						NavigateBack();
					}
					return;
				}

				Accounts[newModel] = new AccountInfo()
				{
					AccountId = newModel.UserId,
					Creds = newModel.AccessToken,
					Name = newModel.Email
				};

				if (IsSending)
				{
					ModelTransferTo = newModel;
				}
				else
				{
					Model = newModel;
				}

				await InitialAuthorization().ConfigureAwait(false);
			}
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

			SaveLoadCreds.SaveData(Accounts.Values.Select(Serializer.Serialize).ToList());
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

		public override Task AddAccount()
		{
			Authentificate();
			return Task.CompletedTask;
		}

		public override async Task AddAccountAndTransfer()
		{
			IsNeedToAddNewAccount = true;
			await ConfirmSelectAccount();
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteSingleServiceData(Serializer.Serialize(Accounts[Model]));
			Accounts.Remove(Model);

			if (Accounts.Count == 0)
			{
				await Model.Logout();
				Model = new DeezerModel();
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

		public override Task LoadUserAccountInfo(List<string> data)
		{
			foreach (var accountData in data)
			{
				var serviceData = Serializer.Deserialize<AccountInfo>(accountData);
				Accounts.Add(new DeezerModel(), serviceData);
			}

			return Task.CompletedTask;
		}

		public override async Task<bool> IsAccountAuthenticated(MusicServiceBase model, AccountInfo accountInfo)
		{
			model.AccessToken = accountInfo.Creds;
			if ((await model.Initialize()).LoginNavigationState == LoginNavigationState.Done)
			{
				return true;
			}

			SaveLoadCreds.DeleteSingleServiceData(accountInfo.Creds);
			Accounts.Remove(model);
			return false;
		}

		public void Authentificate()
		{
			if (!IsSending)
			{
				MainViewModel.NavigateTo(NavigationKeysChild.Main);
			}

			AuthorizationCodeAuthDeezer auth = new AuthorizationCodeAuthDeezer(SendResponse, DeezerApp.RedirectUrl);

			auth.AuthorizationCodeReceived += AuthorizationCodeReceived;

			auth.Run();

			OpenUrlExtension.OpenUrl(DeezerApp.UrlCode);
		}

		private async void AuthorizationCodeReceived(object sender, AuthCodeResponse response)
		{
			if (!IsSending) NavigateToContent();

			var auth = (AuthorizationCodeAuthDeezer)sender;
			auth.Stop();

			await Web_NavigatingAsync(response.Code, response.State).ConfigureAwait(false);
		}

		#endregion AuthMethods

		#region InitialUpdate

		public override async Task<bool> Initial_Update_Playlists(bool forceUpdate = false)
		{
			try
			{
				var selectedTab = SelectedTab;
				var loadingHandler = selectedTab.GetDetailedLoadingHandler();

				MusConvPlayLists.Clear();
				var result = await InitialUpdateBuilder(() => Model.GetPlaylists(loadingHandler), selectedTab, forceUpdate, overrideException: true);
				MusConvPlayLists.AddRange(selectedTab.MediaItems.Select(x => x as MusConvPlayList));
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("This user's profile is private"))
				{
					await ShowMessage(MessageBoxText.DeezerPrivateProfile, Icon.Warning);
				}
				else if (ex.Message.Contains("OAuthException"))
				{
					await ShowMessage(MessageBoxText.DeezerCorrectPermission, Icon.Warning);
				}
			}

			return true;
		}

		public override Task<bool> Initial_Update_Album(bool forceUpdate = false)
		{
			return Initial_Update_Album_DetailedLoading(forceUpdate);
		}

		public override Task<bool> Initial_Update_Featured(bool forceUpdate = false)
		{
			return Initial_Update_Featured_DetailedLoading(forceUpdate);
		}

		private Task<bool> Initial_Update_Stats(bool forceUpdate)
		{
			return _statsTab.SelectedTile.SelectedContentTab.InitialMethod.Initial_DoWork(forceUpdate);
		}

		private Task<bool> Initial_Update_TopArtists(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => DeezerModel.GetTopArtists(), _artistsStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_TopGenres(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => DeezerModel.GetTopGenres(), _genresStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_TopTracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(() => DeezerModel.GetTopTracks(), _tracksStatsTab.SelectedContentTab, forceUpdate);
		}

		private Task<bool> Initial_Update_TopAlbums(bool forceUpdate)
		{
			var tab = _albumsStatsTab.SelectedContentTab;
			var loadingHandler = tab.GetDetailedLoadingHandler();
			return InitialUpdateBuilder(() => DeezerModel.GetTopAlbums(loadingHandler), tab, forceUpdate);
		}

		public Task<bool> Initial_Update_Recommended_Playlists(bool forceUpdate = false)
		{
			var selectedTab = SelectedTab;
			var loadingHandler = selectedTab.GetDetailedLoadingHandler();
			return InitialUpdateBuilder(() => DeezerModel.GetRecommendedPlaylists(loadingHandler), selectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			return ShowHelp(MessageBoxText.DeezerHelp);
		}

		public override Task Command_ShareOnTwitter(MusConvPlayList simplePlaylist)
		{
			var query = $"https://twitter.com/intent/tweet?text=Playlist \"\"\"{System.Web.HttpUtility.UrlEncode(simplePlaylist.Title)}\"\"\" {Texts.SharedBy}   https://www.deezer.com/playlist/{simplePlaylist.Id}";
			OpenUrlExtension.OpenUrl(query);
			return Task.CompletedTask;
		}

		public override Task Command_ShareOnFacebook(MusConvPlayList simplePlaylist)
		{
			var query = $"https://www.facebook.com/sharer/sharer.php?u=https://www.deezer.com/playlist/{simplePlaylist.Id}&quote=Playlist \"\"\"{simplePlaylist.Title}\"\"\" " + Texts.SharedBy;
			OpenUrlExtension.OpenUrl(query);
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

				await Transfer_DoWork(items[0]).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);
			int indexor = 0;
			var counter = 0;
			try
			{
				foreach (var resultKey in result.Keys)
				{
					var lt = result[resultKey]
						.Select(t => t?.ResultItems?.FirstOrDefault()).Where(t => t is not null).Distinct()
						.ToList().SplitList(1994).ToList();

					if (lt.Count == 0)
					{
						var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
						var createdPlaylist = await ModelTransferTo.CreatePlaylist(createModel).ConfigureAwait(false);
						continue;
					}

					foreach (var l1 in lt)
					{
						var createModel = lt.Count == 1
								? new MusConvPlaylistCreationRequestModel(resultKey)
								: MusConvPlaylistCreationRequestModel.GetForPart(resultKey, lt.IndexOf(l1) + 1);
						var pl = await ModelTransferTo.CreatePlaylist(createModel).ConfigureAwait(false);

						MainViewModel.ResultVM.MediaItemIds.Add(pl.Id);

						foreach (var dz in l1.SplitList(100))
						{
							token.ThrowIfCancellationRequested();
							try
							{
								//Those requests sadly can't be run in parallel because Deezer somewhy partially doesn't add tracks send in such a way 
								await ModelTransferTo.AddTracksToPlaylist(pl, dz).ConfigureAwait(false);
							}
							catch (Exception ex)
							{
								MusConvLogger.LogFiles(ex);
							}
							progressReport.Report(new ReportCount(dz.Count,
								$"Adding \"{result[resultKey][indexor]?.OriginalSearchItem?.Title}\" to playlist \"{resultKey}\"", ReportType.Sending, IsSelfTransfer));
							indexor++;
						}
						indexor = 0;
					}
				}

				progressReport.Report(GetPlaylistsReportCount(result));
			}
			catch (Exception ex)
			{
				counter++;

				//Sometimes Deezer, if make transfer big playlists, to asking repeat authentication and throwing an exception.
				//Then this code repeat authentication and make new transfer.
				//If an exception repeated more than 5 times, then stopped transfer.
				if (counter < 5)
				{
					ModelTransferTo.IsAuthorized = false;
					await IsServiceSelectedAsync().ConfigureAwait(false);
					await Transfer_Send(result, index, progressReport, token);
				}
				else
				{
					MusConvLogger.LogFiles(ex);
				}

			}

		}

		#endregion TransferMethods

		#region InnerMethods

		public static string SendResponse(HttpListenerRequest request)
		{
			return string.Format("<html><script type =\"text/javascript\">window.close();</script>Received verification code. This window can be closed now</html>");
		}

		#endregion InnerMethods
	}

	internal static class DeezerApp
	{
		// romanparkhut@gmail.com deezer developer acc
		public const int AppId = 473282;
		public const string SecretKey = "070e77dd213b4b760379cc3bba13c385";

		public const string RedirectUrl = "http://localhost:24446/";

		public static readonly string UrlCode = $"https://connect.deezer.com/oauth/auth.php?app_id={AppId}&redirect_uri={RedirectUrl}&perms=listening_history,basic_access,offline_access,manage_library,delete_library";
		public static readonly string UrlToken = $"https://connect.deezer.com/oauth/access_token.php?app_id={AppId}&secret={SecretKey}";
	}
}