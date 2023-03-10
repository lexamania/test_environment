using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.MessageBoxManager.Texts;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using MusConv.Lib.Plex.Models.ResourceModels;
using MusConv.Lib.Plex.Models.UserModels;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry.Extensions;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using ReactiveUI;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Abstractions.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class PlexTidalViewModel : SectionViewModelBase
	{
		#region Fields

		private PlexTidalModel PlexTidalModel => Model as PlexTidalModel;
		public ReactiveCommand<ManagedAccount, Unit> SwitchManagedAccountCommand { get; set; }
		public ManagedAccount SelectedManagedAccount { get; set; }
		public bool IsManagedAccountsVisible { get; set; }

		#endregion Fields

		#region Constructors

		public PlexTidalViewModel(MainViewModelBase m) : base(m)
		{
			RegState = RegistrationState.Unlogged;
			Title = "Plex (linked Tidal account)";
			SourceType = DataSource.PlexTidal;
			LogoKey = LogoStyleKey.PlexTidalLogo;
			CurrentVMType = VmType.Service;
			Model = new PlexTidalModel();
			Url = "https://app.plex.tv/auth-form/#!?skipLanding=1&forwardUrl=https://www.plex.tv/?signUp=0";
			BaseUrl = "https://www.plex.tv/";
			IsServiceVisibleAsSource = false;

			this.WhenAnyValue(x => x.Model).Subscribe(x =>
			{
			ReevaluateCurrentManagedAccounts();
			});

			ManagedAccounts.CollectionChanged += (_, _) => ReevaluateCurrentManagedAccounts();

			SwitchManagedAccountCommand = ReactiveCommand.CreateFromTask<ManagedAccount>(SwitchManagedAccount);

			#region Commands
			
			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search,Transfer_Send, m)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, "Tidal Playlists", LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Tidal_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(m, "Tidal Tracks", LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tidal_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region DataProperties

		public ObservableDictionary<MusicServiceBase, Resources> Servers { get; set; } = new();

		public ObservableDictionary<MusicServiceBase, List<ManagedAccount>> ManagedAccounts { get; set; } = new();

		public ObservableCollection<ManagedAccount> CurrentUserManagedAccounts { get; set; } = new();

		#endregion DataProperties

		#region AccountsProperties

		public ObservableDictionary<MusicServiceBase, Resources> Servers { get; set; } = new();

		public ObservableDictionary<MusicServiceBase, List<ManagedAccount>> ManagedAccounts { get; set; } = new();

		public ObservableCollection<ManagedAccount> CurrentUserManagedAccounts { get; set; } = new();

		#endregion AccountsProperties

		#region MainProperties

		public ObservableDictionary<MusicServiceBase, Resources> Servers { get; set; } = new();

		public ObservableDictionary<MusicServiceBase, List<ManagedAccount>> ManagedAccounts { get; set; } = new();

		public ObservableCollection<ManagedAccount> CurrentUserManagedAccounts { get; set; } = new();

		#endregion MainProperties

		#region ItemsProperties

		public ObservableDictionary<MusicServiceBase, Resources> Servers { get; set; } = new();

		public ObservableDictionary<MusicServiceBase, List<ManagedAccount>> ManagedAccounts { get; set; } = new();

		public ObservableCollection<ManagedAccount> CurrentUserManagedAccounts { get; set; } = new();

		#endregion ItemsProperties

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			MainViewModel.NeedLogin = this;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			if (!Model.IsAuthenticated())
			{
				if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
				{
					return false;
				}
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				return false;
			}
			else
			{
				await InitialUpdateForCurrentTab().ConfigureAwait(false);
				return true;
			}
		}

		public override async Task Web_NavigatingAsync(object token, object clientIdentifier)
		{
			if (!Model.IsAuthenticated())
			{
				OnLoginPageLeft();
				await Model.Initialize(token, clientIdentifier).ConfigureAwait(false);

				if (!Model.IsAuthenticated())
				{
					await ShowError(Texts.AuthorizationFailed);
					return;
				}

				SaveLoadCreds.SaveData(new List<string>() { GetSerializedServiceData(token.ToString(), clientIdentifier.ToString()) });
				//UserEmail = s.ToString();

				await InitialAuthorization();
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
				await InitializeManagedAccounts(PlexTidalModel);
				await Initial_Update_Playlists().ConfigureAwait(false);
				OnAuthReceived(new AuthEventArgs(Model));
			}
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));
			LogOutRequired = true;
			SelectedTab = Tabs.FirstOrDefault();

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				foreach (var item in Tabs)
				{
					item.MediaItems.Clear();
				}
				NavigateToBrowserLoginPage();
			});

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			var result = await Model.Initialize(serviceData["Token"], serviceData["ClientIdentifier"]).ConfigureAwait(false);
		
			if (result.LoginNavigationState != LoginNavigationState.Done)
			{
				return false;
			}
			
			await InitialAuthorization();

			return true;
		}

		public async Task SwitchManagedAccount(ManagedAccount account)
		{
			await PlexTidalModel.SwitchManagedAccount(account);
			await InitializeManagedAccounts(PlexTidalModel);
			foreach (var tab in Tabs)
			{
				tab.MediaItems.RemoveRange(tab.MediaItems);
			}
			
			NavigateToContent();
			await InitialUpdateForCurrentTab(true);
		}

		private async Task InitializeManagedAccounts(PlexTidalModel model)
		{
			var managedAccounts = await model.GetManagedAccounts();
			ManagedAccounts[model] = managedAccounts;
		}

		private void ReevaluateCurrentManagedAccounts()
		{
			CurrentUserManagedAccounts.Clear();
			if (ManagedAccounts.ContainsKey(Model))
			{
				var userAccounts = ManagedAccounts[Model];
				SelectedManagedAccount = PlexTidalModel.GetCurrentManagedAccount(userAccounts);

				CurrentUserManagedAccounts.AddRange(userAccounts.Where(x => x != SelectedManagedAccount));
			}
			else
			{
				SelectedManagedAccount = null;
			}
			CheckManagedAccountsVisibility();
		}

		private void CheckManagedAccountsVisibility()
		{
			IsManagedAccountsVisible = CurrentUserManagedAccounts.Count > 0;
		}

		#endregion AuthMethods

		#region InitialUpdate

		private Task<bool> Initial_Update_Tidal_Tracks(bool forceUpdate)
		{
			return InitialUpdateBuilder(PlexTidalModel.GetFavorites, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_Tidal_Playlists(bool forceUpdate)
		{
			return InitialUpdateBuilder(PlexTidalModel.GetPlaylists, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region Commands

		private Task Command_Help(object arg1, IProgress<ReportCount> arg2)
		{
			return ShowHelp(MessageBoxText.PlexHelp);
		}

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			return ShowHelp(MessageBoxText.PlexHelp);
		}

		public override Task Command_OpenTracksPage(object arg)
		{
			return OpenPlaylist(NavigationKeysChild.PlexTracks, arg);
		}

		#endregion Commands

		#region TransferMethods

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);

			foreach (var resultKey in result.Keys)
			{
				var requestTracks = result[resultKey].FirstOrDefault(t => t?.ResultItems?.Count > 0)?.ResultItems;
				var requestModel = new MusConvPlaylistCreationRequestModel(resultKey, requestTracks);
				var createdPlaylist = await Model.CreatePlaylist(requestModel).ConfigureAwait(false);

				if (string.IsNullOrEmpty(createdPlaylist.Id))
					continue;

				MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

				foreach (var tracks in result[resultKey].Where(t => t?.ResultItems?.Count > 0).ToList().SplitList(500))
				{
					try
					{
						var selectedTracks = tracks.Select(x => x.ResultItems.First()).ToList();

						token.ThrowIfCancellationRequested();
						await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(new ReportCount(selectedTracks.Count,
							$"Adding tracks to playlist \"{resultKey}\", please wait", ReportType.Sending)));

						await Model.AddTracksToPlaylist(createdPlaylist, selectedTracks).ConfigureAwait(false);
					}
					catch (InvalidOperationException e)
					{
						var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
						await ShowError(e.Message);
						MusConvLogger.LogFiles(sentryEvent);
						await Dispatcher.UIThread.InvokeAsync(() =>
							progressReport.Report(new ReportCount(1, "Error adding tracks...", ReportType.Sended)));
						return;
					}
					catch (Exception ex)
					{
						MusConvLogger.LogFiles(ex);
					}
				}
			}

			await ShowMessage("Songs were transferred to the Tidal account linked to your Plex account");

			await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(result)));
		}

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthenticated())
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			}
			else
			{
				WaitAuthentication.Set();
				IsSending = false;
			};

			await Transfer_DoWork(items[0]);
		}

		#endregion TransferMethods

		#region InnerMethods

		private string GetSerializedServiceData(string login, string password)
		{
			return Serializer.Serialize(new Dictionary<string, string>
			{
				{ "Token", login},
				{ "ClientIdentifier", password},
			});
		}

		public async Task<List<Resources>> GetAliveServers()
		{
			var aliveServers = await (Model as PlexModel).GetAliveServers();
			return aliveServers;
		}

		public async Task LoadUserServers()
		{
			Servers.Clear();
			var servers = await GetAliveServers();
			foreach (var server in servers)
			{
				Servers.Add(new PlexModel(), server);
			}
		}

		#endregion InnerMethods
	}
}