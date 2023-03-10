using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.EventArguments;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class AudiusViewModel : SectionViewModelBase
	{
		#region Fields

		public bool IsBrowserSubscriptionNeeded { get; set; } = true;
		public bool IsLoginInProgress { get; set; }
		public bool RedirectBrowserToUserPage { get; set; }

		#endregion Fields

		#region Constructors

		public AudiusViewModel(MainViewModelBase mainViewModel) : base(mainViewModel)
		{
			Title = "Audius";
			SourceType = DataSource.Audius;
			LogoKey = LogoStyleKey.AudiusLogo;
			SideLogoKey = LeftSideBarLogoKey.AudiusSideLogo;
			SmallLogoKey = LogoStyleKey.AudiusLogoSmall;
			//API doesn`t have methods for add tracks to playlists
			IsSuitableForAutoSync = false;
			Url = "https://audius.co/signin";
			BaseUrl = "https://audius.co/";
			IsTransferAvailable = false;
			Model = new AudiusModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			#region TransferTasks

			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, mainViewModel)
			};

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, mainViewModel)
			};

			#endregion TransferTasks

			#region Tabs

			var discoverWeeklyTab = new TrackTabViewModelBase(mainViewModel, "Your Feed", LwTabIconKey.DiscoverWeeklyTrackIcon, 
				EmptyTransfersBase, commandTracksTab, 
				new Initial_TaskItem("Reload", Initial_Update_Feed), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var repostsTab = new PlaylistTabViewModelBase(mainViewModel, "Reposts", LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Reposts), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(mainViewModel, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase, 
				new LogOut_TaskItem("LogOut", Log_Out));

			var playlistsTab = new PlaylistTabViewModelBase(mainViewModel, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(discoverWeeklyTab);
			Tabs.Add(tracksTab);
			Tabs.Add(repostsTab);
		}

		#endregion Constructors

		#region AuthMethods

		public async override Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (Model.IsAuthorized)
				{
					await InitialUpdateForCurrentTab().ConfigureAwait(false);
					return true;
				}

				if (SaveLoadCreds.IsCredsExists(out var data) && await IsServiceDataExecuted(data))
					return true;
			
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}

			return false;
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			await Initialize(s).ConfigureAwait(false);
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			try
			{
				var audiusData = Serializer.Deserialize<string>(data[Title].FirstOrDefault());
				await Initialize(audiusData);
				return Model.IsAuthorized;
			}
			catch
			{
				return false;
			}
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));

			UserEmail = string.Empty;
			IsBrowserSubscriptionNeeded = true;
			
			foreach (var item in Tabs)
			{
				item.MediaItems.Clear();
			}
			
			await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);

			return true;
		}

		private async Task Initialize(object link)
		{
			try
			{
				await Model.AuthorizeAsync(link, null);

				if (!Model.IsAuthorized)
					return;
				
				//RedirectBrowserToUserPage = true;
				UserEmail = (Model as AudiusModel).Client.User?.name;

				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				OnLoginPageLeft();
				await InitialAuthorization().ConfigureAwait(false);
				OnAuthReceived(new AuthEventArgs(Model));
				SaveLoadCreds.SaveData(new List<string> { Serializer.Serialize(link.ToString()) });
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}
		}

		#endregion AuthMethods

		#region InitialUpdate

		private Task<bool> Initial_Update_Feed(bool forceUpdate)
		{
			return InitialUpdateBuilder((Model as AudiusModel).GetFeed, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_Reposts(bool forceUpdate)
		{
			return InitialUpdateBuilder((Model as AudiusModel).GetReposts, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate
	}
}