using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Lib.ExternalApiClient.Models;
using MusConv.Lib.JamendoAPI;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class JamendoViewModel : SectionViewModelBase
	{
		#region Fields

		private JamendoModel CurrentModel { get; set; }

		#endregion Fields

		#region Constructors

		public JamendoViewModel(MainViewModelBase main) : base(main)
		{
			Title = "Jamendo";
			SourceType = DataSource.Jamendo;
			LogoKey = LogoStyleKey.JamendoLogo;
			SmallLogoKey = LogoStyleKey.JamendoLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.JamendoSideLogo;
			CurrentVMType = VmType.Service;
			RegState = RegistrationState.Unlogged;
			//Service cannot be a destination for autosync due to unable adding track in existing playlist
			IsSuitableForAutoSync = false;

			CurrentModel = new JamendoModel(OnTokenRefreshed);
			Model = CurrentModel;

			BaseUrl = "https://www.jamendo.com/";
			Url = Model.Url;

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);
			
			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
			};

			#endregion Commands

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(main, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(main, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));
			var artistsTab = new ArtistTabViewModelBase(main, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var albumsTab = new AlbumTabViewModelBase(main, LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(tracksTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task SelectServiceAsync()
		{
			MainViewModel.NeedLogin = this;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			if (!Model.IsAuthorized)
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
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
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (!Model.IsAuthorized)
				{
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
					{
						return false;
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

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{				
				await Model.AuthorizeAsync(s as string, null).ConfigureAwait(false);

				if (!Model.IsAuthorized)
				{
					await ShowMessage("Authorization failed", Icon.Error);
					return;
				}
				SaveLoadCreds.SaveData(new List<string> { Serializer.Serialize(CurrentModel.GetAuthData()) });

				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				OnLoginPageLeft();

				await InitialAuthorization();
				Initial_Setup();
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				Debug.WriteLine(e);
				SaveLoadCreds.DeleteServiceData();
			}
		}

		public override bool IsAuthenticated()
		{
			return Model.IsAuthenticated();
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			SaveLoadCreds.DeleteServiceData();
			OnLogoutReceived(new AuthEventArgs(Model));

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				foreach (var tab in Tabs)
				{
					tab.MediaItems.Clear();
				}
				NavigateToMain();
			});

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var authData = Serializer.Deserialize<JamendoCredentials>(data[Title].FirstOrDefault());
			CurrentModel.SetTokens(authData.AccessToken, authData.RefreshToken);
			await InitialAuthorization().ConfigureAwait(false);
			return Model.IsAuthorized;
		}

		#endregion AuthMethods

		#region InnerMethods

		private void OnTokenRefreshed(object sender, TokenRefreshEventArgs e)
		{
			SaveLoadCreds.SaveData(new List<string> { Serializer.Serialize(CurrentModel.GetAuthData()) });
		}

		#endregion InnerMethods
	}
}