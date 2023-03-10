using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Abstractions.Extensions;
using MusConv.MessageBoxManager.Enums;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Sentry.Extensions;
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
using System.Threading;
using System.Threading.Tasks;
using Tidl.Methods;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class TidalViewModel : ShareableServiceViewModelBase, IHighlightableCommands
	{
		#region Fields

		public Dictionary<TabViewModelBase, Command_TaskItem[]> HighlightableCommands { get; }

		#endregion Fields

		#region Constructors

		public TidalViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Tidal";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Tidal;
			CurrentVMType = VmType.Service;
			LogoKey = LogoStyleKey.TidalLogo;
			SideLogoKey = LeftSideBarLogoKey.TidalSideLogo;
			Model = new TidalModel();
			IsMultipleAccountsSupported = true;
			BaseUrl = "https://tidal.com/";
			ArtistDirectUrl = "https://tidal.com/browse/artist/";
			AlbumDirectUrl = "https://tidal.com/browse/album/";

			#region Commands

			Command_ChangeAccount = ReactiveCommand.CreateFromTask<MusicServiceBase>(ChangeAccount);

			Command_ConfirmSelectAccount = ReactiveCommand.CreateFromTask(ConfirmSelectAccount);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnTidalCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnTidalCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new AddTracksCommand(Command_AddTrack, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnTidalCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			var commandRelatedTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnTidalCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ViewOnTidalCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			var commandBarCreateSmartPlaylistCommand = new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.CommandBar);
			var playlistCommandsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
				new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
				new EditCommand(Edit_Confirm),
				new EditCommand(Command_Edit, CommandTaskType.CommandBar),
				new SplitCommand(Command_Split, CommandTaskType.CommandBar),
				new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.DropDownMenu),
				new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.DropDownMenu),
				new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.CommandBar),
				new SortCommand(Command_Sort, CommandTaskType.CommandBar),
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
				commandBarCreateSmartPlaylistCommand,
				new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.DropDownMenu),
				new SharePlaylistCommand(Command_Playlist_Share, CommandTaskType.CommandBar),
				new SharePlaylistCommand(Command_Playlist_Share, CommandTaskType.DropDownMenu),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewOnTidalCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			#endregion

			#region Transfer Tasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_SearchWithAlbums, Transfer_Send, m, true)
			};
			var albumTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
			var artistsTransfer = new List<TaskBase_TaskItem>
			{
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send, m, true)
			};

			#endregion

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, playlistCommandsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandPlaylistTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistsTransfer, commandArtistsTab, 
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

			var youMightAlsoLikeTab = new TrackTabViewModelBase(m, AppTabs.YouMightAlsoLike, LwTabIconKey.YouMightAlsoLikeTrackIcon, 
				trackTransfer, commandRelatedTab,
				new Initial_TaskItem("Reload", Initial_Update_YouMightAlsoLike), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
			Tabs.Add(youMightAlsoLikeTab);

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
				if (ex.Message == "You took too long to log in")
				{
					await ShowWarning(Texts.TidalLogInExpired);
				}
				MusConvLogger.LogFiles(ex);
				Initial_Setup();
				return false;
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

			SaveLoadCreds.SaveData(Accounts.Values.Select(Serializer.Serialize).ToList());
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
				Model = new TidalModel();
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

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				foreach (var tab in Tabs)
				{
					tab.MediaItems.Clear();
				}
				NavigateToMain();
				Initial_Setup();
			});

			return true;
		}

		public override async Task LoadUserAccountInfo(List<string> data)
		{
			foreach (var accountData in data)
			{
				var serviceData = Serializer.Deserialize<AccountInfo>(accountData);

				if (serviceData.Creds == null)
					continue;

				var client = Serializer.Deserialize<ClientConfiguration>(serviceData.Creds);

				var model = new TidalModel();
				var authorizationResult = await model.Initialize(new OpenTidlClient(client));

				if (authorizationResult.LoginNavigationState == LoginNavigationState.Done)
				{
					Accounts.Add(model, serviceData);
				}
			}
		}

		public override async Task<bool> IsAccountAuthenticated(MusicServiceBase model, AccountInfo accountInfo)
		{
			var client = Serializer.Deserialize<ClientConfiguration>(accountInfo.Creds);

			if ((await model.Initialize(new OpenTidlClient(client))).LoginNavigationState == LoginNavigationState.Done)
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

		private async void Authentificate()
		{
			if (!IsSending) MainViewModel.NavigateTo(NavigationKeysChild.Main);

			try
			{
				var authData = await OpenTidlClient.LoginWithLink();
				MainViewModel.MinimizeWindow();
				OpenUrlExtension.OpenUrl("https://" + authData.VerificationUriComplete);
				var token = await OpenTidlClient.WaitForLogin(authData);
				MainViewModel.NormalizeWindow();

				if (!IsSending) NavigateToContent();

				OnLoginPageLeft();

				var newModel = Model.IsAuthenticated() ? new TidalModel() : Model;

				var api = new OpenTidlClient(new ClientConfiguration(token, authData));

				var authorizationResult = await newModel.Initialize(api);
				if (authorizationResult.LoginNavigationState != LoginNavigationState.Done)
				{
					Authentificate();
					return;
				}

				Accounts[newModel] = new AccountInfo()
				{
					AccountId = api.Configuration.Credentials.User.UserId.ToString(),
					Creds = Serializer.Serialize(api.Configuration),
					Name = api.Configuration.Credentials.User.Username
				};

				if (IsSending)
					ModelTransferTo = newModel;
				else
					Model = newModel;

				await InitialAuthorization().ConfigureAwait(false);
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
			}
		}

		#endregion AuthMethods

		#region InitialUpdate

		public override Task<bool> Initial_Update_Playlists(bool forceUpdate = false)
		{
			return Initial_Update_Playlists_DetailedLoading(forceUpdate);
		}

		public override Task<bool> Initial_Update_Album(bool forceUpdate = false)
		{
			return Initial_Update_Album_DetailedLoading(forceUpdate);
		}

		#endregion InitialUpdate

		#region Commands

		public override Task Command_ShareOnTwitter(MusConvPlayList playList)
		{
			var query = $"https://twitter.com/intent/tweet?text=Playlist \"\"\"{System.Web.HttpUtility.UrlEncode(playList.Title)}\"\"\" {Texts.SharedBy}   https://tidal.com/browse/playlist/{playList.Id}";
			OpenUrlExtension.OpenUrl(query);
			return Task.CompletedTask;
		}

		public override Task Command_ShareOnFacebook(MusConvPlayList playList)
		{
			var query = $"https://www.facebook.com/sharer/sharer.php?u=https://tidal.com/browse/playlist/{playList.Id}&quote=Playlist \"\"\"{playList.Title}\"\"\" " + Texts.SharedBy;
			OpenUrlExtension.OpenUrl(query);
			return Task.CompletedTask;
		}

		#endregion Commands

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			if (TabViewModelBase.TransferItemsCount > 500)
			{
				var buttonResult = await ShowWarningDialog(
					Texts.TidalLargeTransferWarning,
					buttons: new[] { ButtonEnum.YesNo },
					title: "MusConv - Tidal transfer warning");

				if (buttonResult != ButtonResult.Yes)
					return;
			}

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

				await Task.Run(() => Transfer_DoWork(items[0])).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
			}
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
		IProgress<ReportCount> progressReport, CancellationToken token)
		{
			try
			{
				MainViewModel.ResultVM.SetPlaylistSearchItem(result);

				foreach (var resultKey in result.Keys)
				{
					var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
					var createdPlaylist = await ModelTransferTo.CreatePlaylist(createModel).ConfigureAwait(false);
					MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

					progressReport.Report(new ReportCount(0, $"Adding tracks to playlist \"{resultKey}\", please wait",
						ReportType.Sending));

					// Splitting list into several lists with 500 max items (optimal value)
					// in order to not send all the tracks to the server at once
					foreach (var ids in result[resultKey].Where(t => t?.ResultItems?.Count > 0)
											.Select(x => x.ResultItems.FirstOrDefault()).ToList()
											.SplitList(500))
					{
						token.ThrowIfCancellationRequested();

						try
						{
							//Those requests sadly can't be run in parallel because YoutubeMusic somewhy partially doesn't add tracks send in such a way
							await ModelTransferTo.AddTracksToPlaylist(createdPlaylist, ids).ConfigureAwait(false);
						}
						catch (Exception ex)
						{
							MusConvLogger.LogFiles(ex);
						}

						progressReport.Report(new ReportCount(ids.Count,
							$"Adding tracks to playlist \"{resultKey}\", please wait",
							ReportType.Sending));
					}
				}

				progressReport.Report(GetPlaylistsReportCount(result));
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
			}
		}

		#endregion TransferMethods
	}
}