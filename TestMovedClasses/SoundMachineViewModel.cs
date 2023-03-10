using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using static MusConv.MessageBoxManager.MessageBox;
using System.Threading;
using System.Threading.Tasks;
using MusConv.MessageBoxManager.Enums;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class SoundMachineViewModel : SectionViewModelBase
	{
		#region Constructors

		public SoundMachineViewModel(MainViewModelBase m) : base(m)
		{
			Title = "SoundMachine";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.SoundMachine;
			LogoKey = LogoStyleKey.SoundMachineLogo;
			SideLogoKey = LeftSideBarLogoKey.SoundMachineSideLogo;
			CurrentVMType = VmType.Service;
			Model = new SoundMachineModel();
			BaseUrl = "https://sound-machine.com/";

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			var transferPlaylist = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m)
			};

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon,
				transferPlaylist, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			Tabs.Add(playlistsTab);
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
				await InitialUpdateForCurrentTab(true).ConfigureAwait(false);
			}
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			await Model.AuthorizeAsync(s.ToString(), t.ToString()).ConfigureAwait(false);

			if (!Model.IsAuthorized)
			{
				await ShowMessage("Authorization failed", Icon.Error);
				return;
			}

			NavigateToContent();
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
				foreach (var t in Tabs)
					t.MediaItems.Clear();
				NavigateToEmailPasswordLoginForm();
			});

			return true;
		}

		#endregion AuthMethods

		#region TransferMethods

		public override Task Transfer_SaveInTo(params object[] items)
		{
			IsSending = true;

			if (!Model.IsAuthorized)
			{
				NavigateToEmailPasswordLoginForm();
			}
			else
			{
				WaitAuthentication.Set();
				IsSending = false;
			}

			return Transfer_DoWork(items[0]);
		}

		private new async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result,
			int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			foreach (var resultKey in result.Keys)
			{
				MusConvPlayList createdPlaylist;
				try
				{
					progressReport.Report(new ReportCount(1,
						$"Wait until the playlist is created...",
						ReportType.Sending));
					var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
					createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);
				}
				catch
				{
					if (result.Keys.Count > 1) continue;

					await ShowMessage($"Failed to create playlist - {resultKey}, please try again.", Icon.Error);
					return;
				}

				if (string.IsNullOrEmpty(createdPlaylist.Id))
				{
					if (result.Keys.Count > 1) continue;

					await ShowMessage($"Failed to create playlist - {resultKey}, please try again.", Icon.Error);
					return;
				}

				var resultTracks = new List<MusConvTrack>();
				foreach (var track in result[resultKey].Where(t => t?.ResultItems?.Count > 0))
				{
					try
					{
						var selectedTrack = track.ResultItems?.FirstOrDefault();
						if (selectedTrack == null) continue;

						token.ThrowIfCancellationRequested();
						progressReport.Report(new ReportCount(1,
							$"Adding \"{track.OriginalSearchItem.Title}\" to playlist \"{resultKey}\"",
							ReportType.Sending));

						resultTracks.Add(selectedTrack);
					}
					catch (Exception ex)
					{
						MusConvLogger.LogFiles(ex);
					}
				}

				try
				{
					await Model.AddTracksToPlaylist(createdPlaylist, resultTracks).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					MusConvLogger.LogFiles(ex);
				}
			}

			progressReport.Report(GetPlaylistsReportCount(result));
		}

		#endregion TransferMethods
	}
}