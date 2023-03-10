using Avalonia.Threading;
using MusConv.Abstractions.Extensions;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Interfaces;
using MusConv.ViewModels.Messages;
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
	public class NapsterViewModel : SectionViewModelBase, IHighlightableCommands
	{
		#region Fields

        public Dictionary<TabViewModelBase, Command_TaskItem[]> HighlightableCommands { get; }

		#endregion Fields

		#region Constructors

        public NapsterViewModel(MainViewModelBase m) : base(m)
		{
			Model = new NapsterModel();
			Title = "Napster";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Napster;
			LogoKey = LogoStyleKey.NapsterLogo;
			SmallLogoKey = LogoStyleKey.NapsterLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.NapsterSideLogo;
			CurrentVMType = VmType.Service;
			Url = (Model as NapsterModel).OAuthUrl;
			BaseUrl = "https://us.napster.com/";
			AlbumDirectUrl = "https://play.napster.com/album/";

			#region Commands

            var commandBarCreateSmartPlaylistCommand = new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.CommandBar);
            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
				new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
				new SplitCommand(Command_Split, CommandTaskType.CommandBar),
				new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
				new EditCommand(Command_Edit, CommandTaskType.CommandBar),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
                commandBarCreateSmartPlaylistCommand,
                new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.DropDownMenu),
                new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.DropDownMenu),
				new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.CommandBar),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
			};

			#endregion Commands

			#region TransferTasks

			var transfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				transfer, commandPlaylistsTab,
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
			var historyTab = new TrackTabViewModelBase(m, "History", LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_History), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));
			var favoritesTab = new TrackTabViewModelBase(m, AppTabs.Favorites, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));
			var libraryTab = new TrackTabViewModelBase(m, "Library", LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Particular_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(historyTab);
			Tabs.Add(favoritesTab);
			Tabs.Add(libraryTab);

            HighlightableCommands = new()
            {
                [playlistsTab] = new Command_TaskItem[] { commandBarCreateSmartPlaylistCommand }
            };

            IHighlightableCommands highlightableCommands = this;
            playlistsTab.StoppedLoading += highlightableCommands.OnTabStoppedLoading;
        }

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			MainViewModel.NeedLogin = this;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			if (!Model.IsAuthorized)
			{
				if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
					return false;
				}
				else
				{
					RegState = RegistrationState.Unlogged;
					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
					return false;
				}
			}
			else
			{
				await InitialUpdateForCurrentTab().ConfigureAwait(false);
				return true;
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
			OnLoginPageLeft();
			if (!await Initialize(s))
			{
				return;
			}
			if (!Model.IsAuthorized)
			{
				await ShowMessage("Authorization failed", Icon.Error);
				return;
			}

			SaveLoadCreds.SaveData(new List<string> { s.ToString() });
			await InitialAuthorization();
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			LogOutRequired = true;
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				foreach (var item in Tabs)
				{
					item.MediaItems.Clear();
				}
				NavigateToBrowserLoginPage();
			});

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
            await Model.AuthorizeAsync(data[Title].FirstOrDefault(), string.Empty).ConfigureAwait(false);

			if (Model.IsAuthorized)
			{
				await InitialAuthorization();
                return true;
			}

			return false;
		}

		private async Task<bool> Initialize(object token)
		{
			if (SelectedTab.Lwstyle == LwTabStyleKey.ItemStyleWebURL)
			{
				//Не хотелось дублировать код,но там если использовать напрямую WebUrlViewModel
				//вылазит какой-то сайд эффект и дергает постоянно MediaItems не давая изменить их мне
				var items = await new ImportByLinkModel().GetAllPlaylist(token.ToString().Split('\n')
					.Where(x => !string.IsNullOrEmpty(x)).Select(x => new Uri(x)).ToList()).ConfigureAwait(false);
				SelectedTab.MediaItems?.Clear();
				SelectedTab.MediaItems.AddRange(items.musConvPlayLists);
				Initial_Setup();

				return true;
			}
			else if (token != null)
			{
				await Model.AuthorizeAsync(token.ToString(), "").ConfigureAwait(false);

				return true;
			}
			else
			{
				await IsServiceSelectedAsync().ConfigureAwait(false);
				return false;
			}
		}

		private async Task InitialAuthorizaton(object token)
		{
			if (!Model.IsAuthorized)
			{
				await ShowMessage("Authorization failed", Icon.Error);
				return;
			}

			SaveLoadCreds.SaveData(new List<string>{token.ToString()});
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

		#endregion AuthMethods

		#region InitialUpdate

		public override Task<bool> Initial_Update_Playlists(bool forceUpdate = false)
		{
			return Initial_Update_Playlists_DetailedLoading(forceUpdate);
		}

		public override Task<bool> Initial_Update_Album(bool forceUpdate = false)
		{
			return Initial_Update_Album_DetailedLoading(forceUpdate);
		}

		public Task<bool> Initial_Update_History(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as NapsterModel).GetHistoryTracks, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region TransferMethods

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
		IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);

			foreach (var resultKey in result.Keys)
			{
				var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
				var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);

				if (string.IsNullOrEmpty(createdPlaylist.Id))
					continue;

				MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

				// Splitting list into several lists with 500 max items (optimal value)
				// in order to not send all the tracks to the server at once
				foreach (var tracks in result[resultKey].Where(t => t?.ResultItems?.Count != 0).ToList().SplitList(500))
				{
					try
					{
						var selectedTracks = tracks.Select(x => x.ResultItems.First()).ToList();

						token.ThrowIfCancellationRequested();
						await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(new ReportCount(selectedTracks.Count,
							$"Adding tracks to playlist \"{resultKey}\", please wait", ReportType.Sending)));

						await Model.AddTracksToPlaylist(createdPlaylist, selectedTracks).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						MusConvLogger.LogFiles(ex);
					}
				}

			}

			await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(result)));
		}

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

		#endregion TransferMethods
	}
}