using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class PrimephonicViewModel : SectionViewModelBase
	{
		#region Constructors

		public PrimephonicViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Primephonic";
			SourceType = DataSource.Primephonic;
			LogoKey = LogoStyleKey.PrimephonicLogo;
			SideLogoKey = LeftSideBarLogoKey.PrimephonicSideLogo;
			RegState = RegistrationState.Unlogged;
			Model = new PrimephonicModel();
			CurrentVMType = VmType.Service;
			Url = "https://play.primephonic.com/my-profile";
			BaseUrl = "https://www.primephonic.com/";

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new Command_TaskItem(Commands.ViewOnPrimephonic, CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
				//new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				//new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
			};

			var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new Command_TaskItem(Commands.ViewOnPrimephonic, CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
				//new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				//new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new AddTracksCommand(Command_AddTrack, CommandTaskType.CommandBar),
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new Command_TaskItem(Commands.ViewOnPrimephonic, CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				//new DeleteDuplicatesCommand(Command_Duplicate, CommandTaskType.CommandBar),
				//new DeleteDuplicatesCommand(Command_Duplicate, CommandTaskType.DropDownMenu),
				//new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				////new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				//new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
				//new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
				//new HelpCommand(Command_Help, CommandTaskType.CommandBar),
				//new EditCommand(Command_Edit, CommandTaskType.CommandBar),
				//new SplitCommand(Command_Split, CommandTaskType.CommandBar),
				//new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
				//new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				//new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new Command_TaskItem(Commands.ViewOnPrimephonic, CommandTrack_Open),
				//new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				//new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				//new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				//new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				//new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				//new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new Command_TaskItem(Commands.ViewOnPrimephonic, CommandTrack_Open),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
                new ViewArtistCommand(CommandTrack_OpenArtist),
				//new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				//new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new Command_TaskItem(Commands.ViewOnPrimephonic, CommandTrack_Open),
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_SearchWithAlbums, Transfer_Send, m, true)
			};

			var albumTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandPlaylistTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));
			var addedPlaylistTab = new PlaylistTabViewModelBase(m, AppTabs.AddedPlaylist, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_AddedPlaylists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var recordingsTab = new PlaylistTabViewModelBase(m, "Recordings", LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Recordings), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var worksTab = new TrackTabViewModelBase(m, "Works", LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab, 
				new Initial_TaskItem("Reload", Initial_Update_Works), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab, 
				new Initial_TaskItem("Reload", Initial_Update_Favorites), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(addedPlaylistTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(worksTab);
			Tabs.Add(recordingsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region AuthMethods

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
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			SelectedTab.Loading = true;
			var token =  (string)await Model.AuthorizeAsync(s, t).ConfigureAwait(false);
			if (!Model.IsAuthorized)
			{
				await ShowMessage("Authorization failed", Icon.Error);
				SelectedTab.Loading = false; await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				return;
			}
			SaveLoadCreds.SaveData(new List<string>{GetSerializedServiceData(token)});
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
			}
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			RegState = RegistrationState.Unlogged;
			Model.IsAuthorized = false;
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				ClearAllMediaItems();
				NavigateToBrowserLoginPage();
			});
			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			await Model.Initialize(serviceData["Token"]);
			SaveLoadCreds.SaveData(new List<string>{GetSerializedServiceData(serviceData["Token"])});
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
			}
			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

		public Task<bool> Initial_Update_AddedPlaylists(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as PrimephonicModel).GetAddedPlaylists, SelectedTab, forceUpdate);
		}

		public Task<bool> Initial_Update_Works(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as PrimephonicModel).GetWorks, SelectedTab, forceUpdate);
		}

		public Task<bool> Initial_Update_Recordings(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as PrimephonicModel).GetRecordings, SelectedTab, forceUpdate);
		}

		public Task<bool> Initial_Update_Favorites(bool forceUpdate = false)
		{
			return InitialUpdateBuilder(GetFavorites, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthorized)
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
			foreach (var resultKey in result.Keys)
			{
				var tracks = result[resultKey].Where(t => t.ResultItems != null)
					.Select(i => i.ResultItems.FirstOrDefault()).Where(p => p is not null).ToList();

				var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
				var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);
				await Model.AddTracksToPlaylist(createdPlaylist, tracks);

				progressReport.Report(new ReportCount(tracks.Count,
					$"Adding tracks to playlist \"{resultKey}\", please wait",
					ReportType.Sending));
			}

			progressReport.Report(GetPlaylistsReportCount(result));
		}

		#endregion TransferMethods

		#region InnerMethods

		private string GetSerializedServiceData(string token)
		{
			return Serializer.Serialize(new Dictionary<string, string>
			{
				{ "Token", token}
			});
		}

		private async Task<List<MusConvTrack>> GetFavorites()
		{
			if ((Model as PrimephonicModel).Favorites == null) await Initial_Update_Playlists();
			return (Model as PrimephonicModel).Favorites;
		}

		#endregion InnerMethods
	}
}