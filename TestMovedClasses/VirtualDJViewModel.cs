using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Abstractions.Extensions;
using MusConv.Lib.VirtualDJ.Models;
using MusConv.MessageBoxManager.Texts;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
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
    public class VirtualDJViewModel : SectionViewModelBase
	{
		#region Constructors

		public VirtualDJViewModel(MainViewModelBase m) : base(m)
		{
			Title = "VirtualDJ";
			RegState = RegistrationState.Unlogged;
			Model = new VirtualDJModel();
			SourceType = DataSource.VirtualDJ;
			LogoKey = LogoStyleKey.VirtualDJLogo;
			SideLogoKey = LeftSideBarLogoKey.VirtualDJSideLogo;
			Url = Urls.VirtualDJ;
			IsHelpVisible = true;

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
			};			
			
			var commandSubscriptionsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
			};

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
			
			var subscriptionsTab = new PlaylistTabViewModelBase(m, AppTabs.Subscriptions, LwTabIconKey.SavedPlaylistIcon, 
				EmptyTransfersBase, commandSubscriptionsTab,
				new Initial_TaskItem("Reload", Initial_Update_Subscriptions), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(subscriptionsTab);
		}

		#endregion Constructors

		#region AuthMethods

        public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (!Model.IsAuthenticated())
				{
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data)
						&& await IsServiceDataExecuted(data))
					{
						return true;
					}

					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
					return false;
				}

				await InitialUpdateForCurrentTab().ConfigureAwait(false);
				return true;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = data.FirstOrDefault(x => x.Key == Title).Value;
			var credentials = Serializer.Deserialize<VirtualDJCreds>(serviceData.FirstOrDefault());

			if ((await Model.Initialize(credentials)).LoginNavigationState != LoginNavigationState.Done)
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				return false;
			}

			if (Accounts.Count == 0)
			{
				await LoadUserAccountInfo(serviceData).ConfigureAwait(false);
			}

			await InitialAuthorization();
			return true;
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			OnLoginPageLeft();

			var creds = s as VirtualDJCreds;

			if ((await Model.Initialize(creds)).LoginNavigationState != LoginNavigationState.Done)
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				return;
			}

			var json = Serializer.Serialize(creds);
			await LoadUserAccountInfo(new List<string>() { json }).ConfigureAwait(false);

			await InitialAuthorization();
		}

		public override async Task LoadUserAccountInfo(List<string> data)
		{
			foreach (var accountData in data)
			{
				var credentials = Serializer.Deserialize<VirtualDJCreds>(accountData);

				var newModel = new VirtualDJModel();
				await newModel.Initialize(credentials);

				var accInfo = new AccountInfo()
				{
					Creds = Serializer.Serialize(credentials),
					Name = newModel.Email
				};

				Accounts.Add(newModel, accInfo);
			}
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			Accounts.Remove(Model);
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			LogOutRequired = true;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				foreach (var item in Tabs)
					item.MediaItems.Clear();

				NavigateToMain();
			});

			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

        private Task<bool> Initial_Update_Subscriptions(bool arg)
        {
			return InitialUpdateBuilder((Model as VirtualDJModel).GetSubscriptions, SelectedTab, arg);
        }

		#endregion InitialUpdate

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			return ShowHelp(MessageBoxText.VirtualDJHelp);
		}

		#endregion Commands

		#region SearchMethods

		public override Task<MusConvTrackSearchResult> DefaultSearchAsync(MusConvTrack track, CancellationToken token)
        {
			// VirtualDJ has no possibility to search tracks, it creates them manually
			var result = new MusConvTrackSearchResult(track, new() { track });
			return Task.FromResult(result);
		}

		#endregion SearchMethods

		#region TransferMethods

        public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthenticated())
			{
				if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) || !await IsServiceDataExecuted(data))
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

					foreach (var tracks in result[resultKey]
										.Select(x => x.ResultItems?.FirstOrDefault()).ToList()
										.SplitList())
					{
						token.ThrowIfCancellationRequested();
						await Model.AddTracksToPlaylist(createdPlaylist, tracks).ConfigureAwait(false);

						await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(new ReportCount(tracks.Count,
						$"Adding \"{result[resultKey][indexor++].OriginalSearchItem.Title}\" to playlist \"{resultKey}\" ",
						ReportType.Sending)));
					}

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