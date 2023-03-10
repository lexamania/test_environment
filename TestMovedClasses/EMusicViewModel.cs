using Avalonia.Threading;
using MusConv.Abstractions;
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
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class EMusicViewModel : SectionViewModelBase
	{
		#region Constructors

		public EMusicViewModel(MainViewModelBase m) : base(m)
		{
			Title = "eMusic";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.EMusic;
			LogoKey = LogoStyleKey.EMusicLogo;
			SideLogoKey = LeftSideBarLogoKey.EMusicSideLogo;
			//Service cannot be a destination for auto sync because can't add tracks from request,
			//need to upload(or buy?) by yourself on the service site
			IsSuitableForAutoSync = false;
			CurrentVMType = VmType.Service;
			Model = new EMusicModel();
			BaseUrl = "https://www.emusic.com/";

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
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

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
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
			await Model.AuthorizeAsync(s.ToString(), t.ToString()).ConfigureAwait(false);

			if (!Model.IsAuthorized)
			{
				await ShowError("Authorization failed");
				return;
			}

			SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(s.ToString(), t.ToString()) });
			
			await Dispatcher.UIThread.InvokeAsync(() => NavigateToContent());
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
				SelectedTab = Tabs.FirstOrDefault(x => x.LwTabIcon == LwTabIconKey.PlaylistIcon);
				await Initial_Update_Playlists().ConfigureAwait(false);				
				OnAuthReceived(new AuthEventArgs(Model));
			}
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));

			foreach (var tab in Tabs)
			{
				tab.MediaItems.Clear();
			}
			
			NavigateToEmailPasswordLoginForm();

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			await Web_NavigatingAsync(serviceData["Login"], serviceData["Password"]);

			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

		public override async Task<bool> Initial_Update_Playlists(bool forceUpdate = false)
		{
			var selectedTab = SelectedTab;
			MusConvPlayLists.Clear();
			var result = await InitialUpdateBuilder(Model.GetPlaylists, SelectedTab, forceUpdate);

			if(!await CheckSession())
			{
				return false;
			}

			MusConvPlayLists.AddRange(selectedTab.MediaItems.Select(x => x as MusConvPlayList));
			return result;
		}

		public override async Task<bool> Initial_Update_Album(bool forceUpdate = false)
		{
			var selectedTab = SelectedTab;
			MusConvAlbums.Clear();
			var result = await InitialUpdateBuilder(Model.GetAlbums, SelectedTab, forceUpdate);

			if (!await CheckSession())
			{
				return false;
			}

			MusConvAlbums.AddRange(selectedTab.MediaItems.Select(x => x as MusConvAlbum));
			return result;
		}

		public override async Task<bool> Initial_Update_Artists(bool forceUpdate = false)
		{
			var result = await InitialUpdateBuilder(Model.GetArtists, SelectedTab, forceUpdate);

			if (!await CheckSession())
			{
				return false;
			}

			return result;
		}

		#endregion InitialUpdate

		#region InnerMethods

		private string GetSerializedServiceData(string login, string password)
		{
			return Serializer.Serialize(new Dictionary<string, string>
								{
									{ "Login", login},
									{ "Password", password},
								});
		}

		private async Task<bool> CheckSession()
		{
			if (!Model.IsAuthorized)
			{
				await Log_Out();
				await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
				return false;
			}
			return true;
		}

		#endregion InnerMethods
	}
}