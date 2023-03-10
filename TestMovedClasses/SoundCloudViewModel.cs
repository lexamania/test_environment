using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Abstractions.Extensions;
using MusConv.MessageBoxManager.Texts;
using MusConv.Sentry;
using MusConv.Sentry.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Interfaces;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Sort;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.WebViewViewModels.AuthHandlers;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.WebViewViewModels
{
	public class SoundCloudViewModel : WebViewModelBase, IHighlightableCommands
	{
		#region Fields

		public Dictionary<TabViewModelBase, Command_TaskItem[]> HighlightableCommands { get; }

		#endregion Fields

		#region Constructors

		public SoundCloudViewModel(MainViewModelBase m) : base(m, new(() => new SoundCloudAuthHandler()))
		{
			Title = "SoundCloud";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.SoundCloud;
			LogoKey = LogoStyleKey.SoundCloudLogo;
			SmallLogoKey = LogoStyleKey.SoundCloudLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.SoundCloudSideLogo;
			Model = new SoundCloudModel();
			BaseUrl = "https://soundcloud.com/";
			Url = "https://soundcloud.com/signin?redirect_url=/discover";
			IsMultipleAccountsSupported = true;
			ArtistDirectUrl = "https://soundcloud.com/";
			AlbumDirectUrl = "https://soundcloud.com/";
			SearchUrl = "https://soundcloud.com/search?q=";

			Command_ChangeAccount = ReactiveCommand.CreateFromTask<MusicServiceBase>(ChangeAccount);
			Command_ConfirmSelectAccount = ReactiveCommand.CreateFromTask(ConfirmSelectAccount);

			#region Commands

			var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnSoundCloudCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new AddTracksCommand(Command_AddTrack, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnSoundCloudCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnSoundCloudCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				//new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				//new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new UploadCommand(Command_Upload, CommandTaskType.CommandBar),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			var commandRelatedTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnSoundCloudCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewOnSoundCloudCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandBarCreateSmartPlaylistCommand = new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.CommandBar);
			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				//new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				//new DeleteDuplicatesCommand(Command_Duplicate, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
				//new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				//new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
				new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
				new EditCommand(Command_Edit, CommandTaskType.CommandBar),
				new EditCommand(Edit_Confirm),
				new SplitCommand(Command_Split, CommandTaskType.CommandBar),
				new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.DropDownMenu),
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
				commandBarCreateSmartPlaylistCommand,
				new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.DropDownMenu),
			};

			var commandStationsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m)
			};
			var albumTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
			var artistsTransfer = new List<TaskBase_TaskItem>
			{
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandPlaylistTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistsTransfer, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var stationsTab = new PlaylistTabViewModelBase(m, AppTabs.Stations, LwTabIconKey.SavedPlaylistIcon, 
				EmptyTransfersBase, commandStationsTab,
				new Initial_TaskItem("Reload", Initial_Update_Stations), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var repostsTab = new TrackTabViewModelBase(m, "Reposts", LwTabIconKey.MixedForYouPlaylistIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Reposts), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var topTracksTab = new TrackTabViewModelBase(m, "Popular Tracks", LwTabIconKey.StatsIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_PopularTracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var historyTab = new PlaylistTabViewModelBase(m, "History", LwTabIconKey.RecommendedPlaylistIcon, 
				EmptyTransfersBase, commandStationsTab,
				new Initial_TaskItem("Reload", Initial_Update_History), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var youMightAlsoLikeTab = new TrackTabViewModelBase(m, AppTabs.YouMightAlsoLike, LwTabIconKey.YouMightAlsoLikeTrackIcon, 
				trackTransfer, commandRelatedTab,
				new Initial_TaskItem("Reload", Initial_Update_YouMightAlsoLike), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(tracksTab);
			Tabs.Add(artistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(stationsTab);
			Tabs.Add(repostsTab);
			Tabs.Add(topTracksTab);
			Tabs.Add(historyTab);
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
			IsSending = false;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			if (!Model.IsAuthenticated())
			{
				if (await IsAnyAccountAuthenticated() ||
					SaveLoadCreds.IsCredsExists(out var data) && await IsServiceDataExecuted(data))
				{
					await InitialAuthorization().ConfigureAwait(false);
					return true;
				}

				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			}
			else
			{
				await InitialUpdateForCurrentTab().ConfigureAwait(false);
			}

			return true;
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			OnLoginPageLeft();

			var eventArgs = s as SoundCloudEventArgs;
			var clientId = eventArgs.Creds.ClientId;
			var authToken = eventArgs.Creds.AuthToken;
			var newModel = Model.IsAuthenticated() ? new SoundCloudModel() : Model;

			if (!await IsModelAuthenticated(newModel as SoundCloudModel, clientId, authToken))
			{
				await ShowError("Authorization failed");
				await Dispatcher.UIThread.InvokeAsync(NavigateToMain);
				return;
			}

			Accounts[newModel] = new AccountInfo()
			{
				AccountId = newModel.UserId,
				Creds = Serializer.Serialize(new Dictionary<string, string>
				{
					{ "ClientId", clientId },
					{ "AuthToken", authToken },
					{ "UserId", newModel.Email }
				}),
				Name = newModel.Email
			};

			if (IsSending)
				ModelTransferTo = newModel;
			else
				Model = newModel;

			await InitialAuthorization().ConfigureAwait(false);
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
			if (!Accounts.ContainsKey(Model)) return true;
			SaveLoadCreds.DeleteSingleServiceData(Accounts[Model].Creds);
			Accounts.Remove(Model);

			if (Accounts.Count == 0)
			{
				await Model?.Logout();
				Model = new SoundCloudModel();
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

			await Dispatcher.UIThread.InvokeAsync(() => NavigateToMain());
			return true;
		}

		public override async Task AddAccount()
		{
			await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
		}

		public override async Task AddAccountAndTransfer()
		{
			IsNeedToAddNewAccount = true;
			await ConfirmSelectAccount();
		}

		public override async Task<bool> IsAccountAuthenticated(MusicServiceBase model, AccountInfo accountInfo)
		{
			var serviceData = Serializer.Deserialize<Dictionary<string, string>>(accountInfo.Creds);

			if (await IsModelAuthenticated(model as SoundCloudModel, serviceData["ClientId"], serviceData["AuthToken"]))
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
				var serviceData = Serializer.Deserialize<Dictionary<string, string>>(accountData);
				if (!serviceData.ContainsKey("UserId")) serviceData.Add("UserId", "");
				Accounts.Add(new SoundCloudModel(), new AccountInfo()
				{
					Creds = accountData,
					Name = serviceData["UserId"]
				});
			}

			return Task.CompletedTask;
		}

		private async Task<bool> IsModelAuthenticated(SoundCloudModel model, object clientId, object authToken)
		{
			return model.IsAuthenticated() || (await model.Initialize(clientId, authToken)).LoginNavigationState == LoginNavigationState.Done;
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

		public Task<bool> Initial_Update_Reposts(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as SoundCloudModel).GetReposts, SelectedTab, forceUpdate);
		}

		public Task<bool> Initial_Update_PopularTracks(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as SoundCloudModel).GetPopularTracks, SelectedTab, forceUpdate);
		}

		public Task<bool> Initial_Update_History(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as SoundCloudModel).GetHistory, SelectedTab, forceUpdate);
		}

		public Task<bool> Initial_Update_Stations(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as SoundCloudModel).GetStations, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			ShowHelp(MessageBoxText.SoundCloudHelp);

			return Task.CompletedTask;
		}

		#endregion Commands

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (IsNeedToAddNewAccount || ModelTransferTo is null)
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			}
			else if (!await IsAccountAuthenticated(ModelTransferTo, Accounts[ModelTransferTo]))
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
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
			foreach (var resultKey in result.Keys)
			{
				var tracks = result[resultKey].Where(t => t.ResultItems != null)
					.Select(i => i.ResultItems.FirstOrDefault())
					.AllIsNotNull().ToList();

				var tracksSplits = tracks.SplitList(450).ToList();
				foreach (var trackList in tracksSplits)
				{
					try
					{
						var createTitle = tracksSplits.Count == 1 ? resultKey.ToString() : $"{resultKey} №{tracksSplits.IndexOf(trackList) + 1}";
						var createModel = new MusConvPlaylistCreationRequestModel(createTitle, VariableTexts.GetItemDescriptionFromSourceItem(resultKey));

						var createdPlaylist = await ModelTransferTo.CreatePlaylist(createModel);
						
						await ModelTransferTo.AddTracksToPlaylist(createdPlaylist, trackList).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						MusConvLogger.LogFiles(ex);
					}
					progressReport.Report(new ReportCount(trackList.Count,
						$"Adding tracks to playlist \"{resultKey}\", please wait",
						ReportType.Sending, IsSelfTransfer));
				}
			}
			progressReport.Report(GetPlaylistsReportCount(result));
		}

		public override async Task<MusConvTrackSearchResult> Transfer_Search(int index, MusConvTrack track,
		   IProgress<ReportCount> arg3, CancellationToken token)
		{
			try
			{
				var search = track.Title.Contains('-')
					? $"\"{track.Title.Split('-').Last()}\" by \"{track.Title.Split('-').First()}\""
					: $"\"{track.Title}\" by \"{track.Artist}\"";
				arg3.Report(new ReportCount(index, $"Searching: {search} ", ReportType.Searching));
				token.ThrowIfCancellationRequested();
				var result = await DefaultSearchAsync(track, token);

				if (result?.ResultItems != null && result.ResultItems.Count != 0)
					return result;

				// if tracks not found, then trying to search tracks with sort accuracy ignoring
				var tracks = await ModelTransferTo.SearchTrack(new MusConvTrack(){Title = search }, token) ?? new List<MusConvTrack>();
				result = Sort.Sort.DeleteNotValidTracks(
					new MusConvTrackSearchResult(track, tracks),
					MainViewModel.SettingsVM,
					SortTracksExtension.MinimizeAccuracy);

				return result;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return new MusConvTrackSearchResult(track);
			}
		}

		#endregion TransferMethods
	}
}