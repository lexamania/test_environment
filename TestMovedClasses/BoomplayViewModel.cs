using System.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class BoomplayViewModel : SectionViewModelBase
	{
		#region Constructors

		public BoomplayViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Boomplay";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Boomplay;
			LogoKey = LogoStyleKey.BoomplayLogo;
			SideLogoKey = LeftSideBarLogoKey.BoomplaySideLogo;
			CurrentVMType = VmType.Service;
			Model = new BoomplayModel();
			BaseUrl = Urls.Boomplay;
			Url = Urls.Boomplay;
			ArtistDirectUrl = "https://www.boomplay.com/artists/";
			AlbumDirectUrl = "https://www.boomplay.com/albums/";
			SearchUrl = "https://www.boomplay.com/search/default/";

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ViewOnBoomplayCommand(CommandTrack_Open),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
			};

			var commandFolowedPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ViewOnBoomplayCommand(CommandTrack_Open),
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewOnBoomplayCommand(CommandTrack_Open),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ViewOnBoomplayCommand(CommandTrack_Open),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnBoomplayCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
			};
			
			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnBoomplayCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};
			var albumTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
			var artistsTransfer = new List<TaskBase_TaskItem>
			{
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m, true)
			};

			#endregion TransferTasks

			#region Tabs

			var playlsitsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var folowedPlaylistsTab = new PlaylistTabViewModelBase(m, AppTabs.FollowedPlaylists, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandFolowedPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_FolowedPlaylists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistsTransfer, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlsitsTab);
			Tabs.Add(folowedPlaylistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
				{
					return false;
				}
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				return true;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		// s - cookies
		public override async Task Web_NavigatingAsync(object s, object t)
		{
			OnLoginPageLeft();
			await InitialAuthorization(s as List<Cookie>).ConfigureAwait(false);
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			MainViewModel.NeedLogin = this;
			RegState = RegistrationState.Unlogged;
			Model.IsAuthorized = false;
			SaveLoadCreds.DeleteServiceData();
			foreach (var item in Tabs)
			{
				item.MediaItems.Clear();
			}
			await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			return true;
		}

		private async Task InitialAuthorization(List<Cookie> cookies)
		{
			await Model.Initialize(cookies);

			// SaveLoadCreds.SaveData(new List<string> { cookie });
			RegState = RegistrationState.Logged;
			if (IsSending)
			{
				WaitAuthentication.Set();
				IsSending = false;
			}
			else
			{
				await Initial_Update_Playlists();
				OnAuthReceived(new AuthEventArgs(Model));
			}
			Initial_Setup();
		}

		#endregion AuthMethods

		#region InitialUpdate

		private Task<bool> Initial_Update_FolowedPlaylists(bool arg)
		{
			return InitialUpdateBuilder(Model.GetFavoritePlaylists, SelectedTab, true);
		}

		#endregion InitialUpdate

		#region TransferMethods

		public override async Task Transfer_SaveInTo(object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			try
			{
				WaitAuthentication = new AsyncAutoResetEvent();
				if (SelectedAccount != null)

				{
					await ChangeAccount(Accounts.FirstOrDefault(x => x.Value == SelectedAccount).Key);
					IsSelfTransfer = true;
					WaitAuthentication.Set();
					SelectedAccount = null;
				}
				else if (!Model.IsAuthorized)
				{
					if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) ||
						!await IsServiceDataExecuted(data))
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
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}

			await Transfer_DoWork(items[0]).ConfigureAwait(false);

			IsSelfTransfer = false;
			IsSending = false;
			await InitialUpdateForCurrentTab(true).ConfigureAwait(false);
		}

		#endregion TransferMethods
	}
}