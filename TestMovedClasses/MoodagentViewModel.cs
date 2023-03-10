using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Abstractions.Extensions;
using MusConv.ViewModels.Settings;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class MoodagentViewModel : SectionViewModelBase
	{
		#region Constructors

		public MoodagentViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Moodagent";
			SourceType = DataSource.Moodagent;
			LogoKey = LogoStyleKey.MoodagentLogo;
			RegState = RegistrationState.Unlogged;
			CurrentVMType = VmType.Service;
			Url = "https://account.moodagent.com/sign-in";
			BaseUrl = "https://moodagent.com/";
			Model = new MoodagentModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase);

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon,
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon,
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon,
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				if (string.IsNullOrEmpty((Model as MoodagentModel).MoodClient.User.UserToken))
				{
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) &&
						await IsServiceDataExecuted(data))
					{
						return false;
					}

					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
					return false;
				}

				Model.IsAuthorized = true;

				return true;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			OnLoginPageLeft();
			var token = await (Model as MoodagentModel).AuthorizeWithToken(s.ToString(), t.ToString());
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			await InitialAuthorization(token).ConfigureAwait(false);
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
				await Initial_Update_Playlists();
			}

			Initial_Setup();
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			SetToken(data[Title].FirstOrDefault());
			if (!await Model.IsSavedAuthDataValid())
			{
                if (SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.LogOutOnLogInError))
                {
                    (Model as MoodagentModel).MoodClient.User.UserToken = null;
                }
                else
                {
                    await ShowAuthorizationError();
                }
				return false;
			}
			else
			{
				await InitialAuthorization(data[Title].FirstOrDefault()).ConfigureAwait(false);
				return true;
			}
		}

		private async Task<bool> Log_Out(bool forceUpdate = false)
		{
			LogOutRequired = true;
			SaveLoadCreds.DeleteServiceData();
			RegState = RegistrationState.Unlogged;
			Model.IsAuthorized = false;
			foreach (var item in Tabs)
			{
				item.MediaItems.Clear();
			}

			await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			return true;
		}

		#endregion AuthMethods

		#region TransferMethods

		public override async Task Transfer_SaveInTo(object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;
			if (string.IsNullOrEmpty((Model as MoodagentModel).MoodClient.User.UserToken))
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

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result,
			int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);

			foreach (var resultKey in result.Keys)
			{
				var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
				var createdPlaylist = await ModelTransferTo.CreatePlaylist(createModel).ConfigureAwait(false);

				MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);
				foreach (var it in result[resultKey].Where(t => t?.ResultItems != null && t.ResultItems.Count > 0)
							 .Select(x => x.ResultItems.FirstOrDefault()).ToList()
							 .SplitList())
				{
					await ModelTransferTo.AddTracksToPlaylist(createdPlaylist, it);
					progressReport.Report(new ReportCount(it.Count(),
						$"Adding tracks to playlist \"{resultKey}\", please wait",
						ReportType.Sending, IsSelfTransfer));
				}

				await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(result)));
			}
		}

		#endregion TransferMethods

		#region InnerMethods

		private void SetToken(string token)
		{
			(Model as MoodagentModel).MoodClient.User.UserToken = token;
		}

		#endregion InnerMethods
	}
}