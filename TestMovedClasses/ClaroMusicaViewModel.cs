using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Models;
using System.Threading;
using MusConv.ViewModels.Helper;
using System.Linq;
using MusConv.Abstractions;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Abstractions.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Messages;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class ClaroMusicaViewModel: SectionViewModelBase
	{
		#region Fields

		public bool needSubscribe = true;
		public bool needLogout = false;

		#endregion Fields

		#region AuthMethods

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			MainViewModel.NeedLogin = this;
			needLogout = true;
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

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			RegState = RegistrationState.Logged;
			await (Model as ClaroMusicaModel).Login(s.ToString());

			if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data))
				SaveLoadCreds.SaveData(new List<string> { Serializer.Serialize((s.ToString(), DateTime.Now)) });

			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			SelectedTab.Loading = true;
			await SelectedTab.Command_Reload(null, CommandTaskType.DropDownMenu);
		}

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				if ((SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data)) && !Model.IsAuthorized)
				{
					var claroData = Serializer.Deserialize<(string, DateTime)>(data[Title].FirstOrDefault());
					
					//If claro not support saved cookies after some time, just let user relogin
					var twoDaysHolder = claroData.Item2.AddDays(2);
					if (twoDaysHolder > DateTime.Now)
						await Web_NavigatingAsync(claroData.Item1, null);
					else SaveLoadCreds.DeleteServiceData();
				}
				if (Model.IsAuthorized)
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
					return false;
				}
				else
				{
					MainViewModel.NeedLogin = this;
					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
					return false;
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		#endregion AuthMethods

		#region InitialUpdate

		private Task<bool> Initial_Update_FolowedPlaylists(bool arg)
		{
			return InitialUpdateBuilder(Model.GetFavoritePlaylists, SelectedTab, true);
		}

		#endregion InitialUpdate

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
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

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);
			using var manager = QueueProvider.CreateTransferQueueManager();
			foreach (var resultKey in result.Keys)
			{
				var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
				var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);

				progressReport.Report(new ReportCount(0, $"Adding tracks to playlist \"{resultKey}\", please wait",
					ReportType.Sending));
				MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

				var tasks = new List<Task>();
				foreach (var it in result[resultKey].Select(x => x?.ResultItems?.FirstOrDefault())
					.Where(t => !string.IsNullOrEmpty(t?.Id)).ToList().SplitList())
				{
					tasks.Add(manager.Enqueue(() => Model.AddTracksToPlaylist(createdPlaylist, it)));
				}

				await Task.WhenAll(tasks).ConfigureAwait(false);
			}

			await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(result)));
		}

		#endregion TransferMethods

		#region InnerMethods

		public ClaroMusicaViewModel(MainViewModelBase m): base(m)
		{
			Title = "Claro Musica";
			SourceType = DataSource.ClaroMusica;
			LogoKey = LogoStyleKey.ClaroMusicaLogo;
			SideLogoKey = LeftSideBarLogoKey.ClaroMusicaSideLogo;
			Url = "https://www.claromusica.com/login/mx";
			RegState = RegistrationState.Unlogged;
			Model = new ClaroMusicaModel();
			BaseUrl = "https://www.claromusica.com/";

			#region Commands

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewOnSpotifyCommand(CommandTrack_Open),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnSpotifyCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_SearchWithAlbums, Transfer_Send, m)
			};
			var albumTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
			var ArtistTransfer = new List<TaskBase_TaskItem>
			{
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};
			var TrackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var folowedPlaylistsTab = new PlaylistTabViewModelBase(m, AppTabs.FollowedPlaylists, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_FolowedPlaylists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				TrackTransfer, commandTracksTab, 
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				ArtistTransfer, commandArtistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(folowedPlaylistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion InnerMethods
	}
}