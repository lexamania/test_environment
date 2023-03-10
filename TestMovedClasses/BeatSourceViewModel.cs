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
using MusConv.ViewModels.EventArguments;
using MusConv.Abstractions;
using MusConv.Lib.BeatSource;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class BeatSourceViewModel : SectionViewModelBase
	{
		#region Constructors

		public BeatSourceViewModel(MainViewModelBase main) : base(main)
		{
			Title = "BeatSource";
			LoginPassPageFirstField = "Username";
			SourceType = DataSource.BeatSource;
			LogoKey = LogoStyleKey.BeatSourceLogo;
			SideLogoKey = LeftSideBarLogoKey.BeatSourceSideLogo;
			RegState = RegistrationState.Unlogged;
			CurrentVMType = VmType.Service;
			Model = new BeatSourceModel();
			BaseUrl = "https://www.beatsource.com/";

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			#region TransferTasks

			var transferPlaylist = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, main)
			};

			#endregion TransferTasks

			var playlistsTab = new PlaylistTabViewModelBase(main, LwTabIconKey.PlaylistIcon, 
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
				NavigateToEmailPasswordLoginForm();
			}
			else
			{
				await InitialUpdateForCurrentTab().ConfigureAwait(false);
			}
		}

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				NavigateToContent();

				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				if (Model.IsAuthorized == false)
				{
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
					{
						return false;
					}
					await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
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

			OnLoginPageLeft();
			await InitialAuthorization().ConfigureAwait(false);
			SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(s.ToString(), t.ToString()) });
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
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));
			
			foreach (var t in Tabs)
				t.MediaItems.Clear();
			
			await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var authData = Serializer.Deserialize<BeatSourceCredentials>(data[Title].FirstOrDefault());
			await Model.AuthorizeAsync(authData.Login, authData.Password).ConfigureAwait(false);
			UserEmail = authData.Login;

			await InitialAuthorization().ConfigureAwait(false);
			return Model.IsAuthorized;
		}

		#endregion AuthMethods

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthorized)
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
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
				var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
				var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);

				if (string.IsNullOrEmpty(createdPlaylist.Id))
				{
					continue;
				}

				MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

				var tracks = new List<MusConvTrack>();

				foreach (var track in result[resultKey]
					.Where(t => t?.ResultItems != null && t?.ResultItems?.Count != 0))
				{
					try
					{
						var selectedTrack = track?.ResultItems?.FirstOrDefault();

						if (selectedTrack is null)
						{
							continue;
						}

						token.ThrowIfCancellationRequested();
						progressReport.Report(new ReportCount(1,
							$"Adding \"{track.OriginalSearchItem.Title}\" to playlist \"{resultKey}\"",
							ReportType.Sending));

						tracks.Add(selectedTrack);
					}
					catch (Exception ex)
					{
						MusConvLogger.LogFiles(ex);
					}
				}

				await Model.AddTracksToPlaylist(createdPlaylist, tracks).ConfigureAwait(false);
			}

			progressReport.Report(GetPlaylistsReportCount(result));
		}

		#endregion TransferMethods

		#region InnerMethods

		private string GetSerializedServiceData(string login, string password)
		{
			return Serializer.Serialize(new BeatSourceCredentials { Login = login, Password = password });
		}

		#endregion InnerMethods
	}
}