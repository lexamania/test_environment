using Avalonia.Threading;
using MusConv.MessageBoxManager.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusConv.Sentry;
using MusConv.Sentry.Extensions;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.Models;
using System;
using System.Threading;
using MusConv.Abstractions.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class HearthisAtViewModel : SectionViewModelBase
	{
		#region Constructors

		public HearthisAtViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Hearthis.at";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.HearthisAt;
			LogoKey = LogoStyleKey.HearthisAtLogo;
			SideLogoKey = LeftSideBarLogoKey.HearthisAtSideLogo;
			CurrentVMType = VmType.Service;
			BaseUrl = "https://hearthis.at/";

			Model = new HearthisAtModel();

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

			var playlistsTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};
			var tracksTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m, true)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistsTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));			
			
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				tracksTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task SelectServiceAsync()
		{
			MainViewModel.NeedLogin = this;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			if (!Model.IsAuthorized)
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);

			}
			else
			{
				await InitialUpdateForCurrentTab().ConfigureAwait(false);
			}
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				await Model.AuthorizeAsync(s.ToString(), t.ToString()).ConfigureAwait(false);
			}
			catch (System.Net.WebException e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
				return;
			}

			if (!Model.IsAuthorized)
			{
				await ShowMessage("Authorization failed", Icon.Error);
				return;
			}

			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			UserEmail = s.ToString();
			RegState = RegistrationState.Logged;
			if (IsSending)
			{
				WaitAuthentication.Set();
				IsSending = false;
			}
			else
			{
				await Initial_Update_Playlists().ConfigureAwait(false);
			}
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				ClearAllMediaItems();
				NavigateToEmailPasswordLoginForm();
			});

			return true;
		}

		#endregion AuthMethods

		#region TransferMethods

        public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthenticated())
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

		private new async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
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

					foreach (var item in result[resultKey].Where(t => t?.ResultItems?.Count > 0)
										.Select(x => x.ResultItems?.FirstOrDefault()).ToList()
										.SplitList())
					{
						token.ThrowIfCancellationRequested();
						await Model.AddTracksToPlaylist(createdPlaylist, item).ConfigureAwait(false);
						await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(new ReportCount(item.Count,
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