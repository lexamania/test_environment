using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
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
	public class DiscogsViewModel : SectionViewModelBase
	{
		#region Constructors

		public DiscogsViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Discogs";
			LoginPassPageFirstField = "Username";
			LoginPassPageSecondField = "Token";
			LogoKey = LogoStyleKey.DiscogsLogo;
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Discogs;
			SmallLogoKey = LogoStyleKey.DiscogsLogo;
			SideLogoKey = LeftSideBarLogoKey.DiscogsSideLogo;
			CurrentVMType = VmType.Service;
			Model = new DiscogsModel();
			BaseUrl = "https://www.discogs.com/";

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands
			
			#region Tabs
			
			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};

			var playlistTab = new PlaylistTabViewModelBase(m, "Collection", LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			
			var foldersTab = new PlaylistTabViewModelBase(m, "Folders", LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Folders), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			
			#endregion Tabs

			Tabs.Add(foldersTab);
			Tabs.Add(playlistTab);			
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

					MainViewModel.NavigateTo(NavigationKeysChild.EmailPasswordLoginForm);

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
				await ShowError("Authorization failed");
				return;
			}

			SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(s.ToString(), t.ToString()) });
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			OnLoginPageLeft();
			UserEmail = s.ToString();
			RegState = RegistrationState.Logged;
			if (IsSending)
			{
				WaitAuthentication.Set();
				IsSending = false;
			}
			else
			{
				await Initial_Update_Folders().ConfigureAwait(false);
			}
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			await Model.AuthorizeAsync(serviceData["username"], serviceData["token"]);
			await InitialAuthorization();

			return true;
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

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				ClearAllMediaItems();
				MainViewModel.NavigateTo(NavigationKeysChild.EmailPasswordLoginForm);
			});

			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

		public override async Task<bool> Initial_Update_Playlists(bool forceUpdate = false)
		{
			var selectedTab = SelectedTab;
			MusConvPlayLists.Clear();
			var result = await InitialUpdateBuilder(GetCollectionFolders, SelectedTab, forceUpdate);

			MusConvPlayLists.AddRange(selectedTab.MediaItems.Select(x => x as MusConvPlayList));
			return result;
		}

		public async Task<bool> Initial_Update_Folders(bool forceUpdate = false)
		{
			var selectedTab = SelectedTab;
			MusConvPlayLists.Clear();
			var result = await InitialUpdateBuilder(Model.GetPlaylists, SelectedTab, forceUpdate);

			MusConvPlayLists.AddRange(selectedTab.MediaItems.Select(x => x as MusConvPlayList));
			return result;
		}

		#endregion InitialUpdate

		#region Commands

		public Task Command_Help(object arg1)
		{
			MainViewModel.NavigateTo(NavigationKeysChild.DiscogsHelpPage);
			return Task.CompletedTask;
		}

		#endregion Commands

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
				var trackIds = result[resultKey].Where(t => t.ResultItems != null)
					.Select(i => i.ResultItems.FirstOrDefault())
					.Where(p => p is not null).ToList();

				var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
				var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);
				await ModelTransferTo.AddTracksToPlaylist(createdPlaylist, trackIds).ConfigureAwait(false);

				progressReport.Report(new ReportCount(trackIds.Count,
					$"Adding tracks to playlist \"{resultKey}\", please wait",
					ReportType.Sending));
			}
			progressReport.Report(GetPlaylistsReportCount(result));
		}

		#endregion TransferMethods

		#region InnerMethods

		private string GetSerializedServiceData(string userName, string token)
		{
			return Serializer.Serialize(new Dictionary<string, string>
			{
				{"username", userName},
				{"token", token},
			});
		}

		private Task<List<MusConvPlayList>> GetCollectionFolders()
		{
			return (Model as DiscogsModel).GetFolders();
		}

		#endregion InnerMethods
	}
}