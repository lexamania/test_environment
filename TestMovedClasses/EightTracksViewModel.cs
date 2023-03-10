using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Lib.EightTracks;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class EightTracksViewModel : SectionViewModelBase
	{
		#region Constructors

		public EightTracksViewModel(MainViewModelBase m) : base(m)
		{
			Title = "8tracks";
			SourceType = DataSource.EightTracks;
			LogoKey = LogoStyleKey.EightTracksLogo;
			SideLogoKey = LeftSideBarLogoKey.EightTracksSideLogo;
			RegState = RegistrationState.Unlogged;
			CurrentVMType = VmType.Service;
			Model = new EightTracksModel();
			BaseUrl = "https://8tracks.com/";
			//did not find way to add tracks to playlist
			IsSuitableForAutoSync = false;

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
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_SearchWithAlbums, Transfer_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var favoriteTracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase, 
				new LogOut_TaskItem("LogOut", Log_Out));
			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));
			var likedPlaylistTab = new PlaylistTabViewModelBase(m, AppTabs.LikedPlaylists, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Featured), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(likedPlaylistTab);
			Tabs.Add(favoriteTracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
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
			SaveLoadCreds.SaveData(new List<string>{GetSerializedServiceData(s.ToString(), t.ToString())});
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			OnLoginPageLeft();
			UserEmail = s.ToString();

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
				NavigateToEmailPasswordLoginForm();
			});

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<EightTracksCredentials>(data[Title].FirstOrDefault());
			await Web_NavigatingAsync(serviceData.Login, serviceData.Password);

			return true;
		}

		#endregion AuthMethods

		#region InnerMethods

		private string GetSerializedServiceData(string login, string password)
		{
			return Serializer.Serialize(new EightTracksCredentials { Login = login, Password = password });
		}

		#endregion InnerMethods
	}
}