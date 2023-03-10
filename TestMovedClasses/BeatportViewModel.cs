using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Abstractions;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	class BeatportViewModel : SectionViewModelBase
	{
		#region Constructors

		public BeatportViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Beatport";
			LoginPassPageFirstField = "Username";
			SourceType = DataSource.Beatport;
			LogoKey = LogoStyleKey.BeatportLogo;
			SmallLogoKey = LogoStyleKey.BeatportLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.BeatportSideLogo;
			RegState = RegistrationState.Unlogged;
			Model = new BeatportModel();
			CurrentVMType = VmType.Service;
			BaseUrl = "https://www.beatport.com/";
			ArtistDirectUrl = "https://www.beatport.com/artist/";
            SearchUrl = "https://www.beatport.com/search?q=";

			#region Commands

            var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnBeatportCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
				// new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				// new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new AddTracksCommand(Command_AddTrack, CommandTaskType.CommandBar),
			};
			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ViewOnBeatportCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
				new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
				new HelpCommand(Command_Help, CommandTaskType.CommandBar),
				new EditCommand(Command_Edit, CommandTaskType.CommandBar),
				new SplitCommand(Command_Split, CommandTaskType.CommandBar),
				new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
				// new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				// new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnBeatportCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandArtist = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new ViewOnBeatportCommand(CommandTrack_Open),
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtist,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var labelTab = new ArtistTabViewModelBase(m, "Labels", LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtist,
				new Initial_TaskItem("Reload", Initial_Update_Labels), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(m, "Collection", LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(artistsTab);
			Tabs.Add(labelTab);
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

				if (!Model.IsAuthorized)
				{
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data)
						&& await IsServiceDataExecuted(data))
					{
						return false;
					}
					await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
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
			OnLoginPageLeft();
			var accountName = s.ToString();
			if (accountName.Contains('@'))
			{
				await ShowError("Use your Username instead of email");
			}
			SelectedTab.Loading = true;
			await Model.AuthorizeAsync(accountName, t.ToString()).ConfigureAwait(false);
			if (!Model.IsAuthorized)
			{
				await ShowMessage("Authorization failed", Icon.Error);
				SelectedTab.Loading = false;
				await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);

				return;
			}
			SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(s.ToString(), t.ToString()) });
			UserEmail = accountName;

			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			await InitialAuthorization();
		}

		public override async Task InitialAuthorization()
		{
			RegState = RegistrationState.Logged;
			SelectedTab.Loading = false;
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

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));
			
			ClearAllMediaItems();

			await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			await Model.AuthorizeAsync(serviceData["Login"], serviceData["Password"]);
			await InitialAuthorization();

			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

		public Task<bool> Initial_Update_Labels(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as BeatportModel).GetLabels, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthorized)
			{
				if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) || !await IsServiceDataExecuted(data))
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
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

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);

			foreach (var resultKey in result.Keys)
			{
				var trackIds = result[resultKey]
					.Where(t => t.ResultItems != null)
					.Select(i => i.ResultItems.FirstOrDefault())
					.Where(p => p is not null)
					.ToList();

				var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
				var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);

				await Model.AddTracksToPlaylist(createdPlaylist, trackIds);

				progressReport.Report(new ReportCount(trackIds.Count,
					$"Adding tracks to playlist \"{resultKey}\", please wait",
					ReportType.Sending));
			}

			progressReport.Report(GetPlaylistsReportCount(result));
		}

		#endregion TransferMethods

		#region InnerMethods

		private string GetSerializedServiceData(string login, string password)
		{
			return Serializer.Serialize(new Dictionary<string, string>
			{
				{ "Login", login},
				{ "Password", password},
			});
		}

		#endregion InnerMethods
	}
}