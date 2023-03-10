using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.Sentry;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class LiveOneViewModel : SectionViewModelBase
	{
		#region Constructors

		public LiveOneViewModel(MainViewModelBase m) : base(m)
		{
			Title = "LiveXLive( LiveOne )";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.LiveOne;
			LogoKey = LogoStyleKey.LiveOneLogo;
			SideLogoKey = LeftSideBarLogoKey.LiveOneSideLogo;
			CurrentVMType = VmType.Service;
			Url = "https://account.livexlive.com/login";
			BaseUrl = "https://www.livexlive.com/";
			Model = new LiveOneModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

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
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m)
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
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks);

			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks);

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistsTransfer, commandArtistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks);

			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab, 
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase);

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			MainViewModel.NeedLogin = this;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			if (Model.IsAuthenticated() == false)
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
				await WaitAuthentication.WaitAsync().ConfigureAwait(false);
			}
			else
			{
				await InitialUpdateForCurrentTab().ConfigureAwait(false);
			}

			return false;
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				await Model.AuthorizeAsync(s as string, t as string).ConfigureAwait(false);

				RegState = RegistrationState.Logged;

				if (IsSending)
				{
					WaitAuthentication.Set();
					IsSending = false;
				}
				else
				{
					await Initial_Update_Playlist().ConfigureAwait(false);
				}
				Initial_Setup();
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				Debug.WriteLine(e);
			}
		}

		#endregion AuthMethods

		#region InitialUpdate

		private async Task<bool> Initial_Update_Playlist(bool forceUpdate = false)
		{

			SelectedTab.LoadingText = MusConvConfig.PlayListLoading;
			SelectedTab.Loading = true;
			try
			{
				if (SelectedTab == null || SelectedTab.MediaItems == null || SelectedTab.MediaItems.Count == 0 || forceUpdate)
				{
					var items = await Model.GetPlaylists().ConfigureAwait(false);

					if (items != null)
					{
						SelectedTab.MediaItems?.Clear();
						SelectedTab.MediaItems.AddRange(items);
					}
				}
			}
			catch (Exception ex)
			{
				UnableToLoadPlaylists(ex);
				MusConvLogger.LogFiles(ex);
				Initial_Setup();
			}
			Initial_Setup();
			return true;
		}

		#endregion InitialUpdate

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthenticated())
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			}
			else
			{
				WaitAuthentication.Set();
				IsSending = false;
			}
			await Transfer_DoWork(items[0], true).ConfigureAwait(false);
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
		IProgress<ReportCount> progressReport, CancellationToken token)
		{
			foreach (var resultKey in result.Keys)
			{
				try
				{
					var tracks = result[resultKey].Select(x => x.ResultItems.FirstOrDefault());
					await Model.CreatePlaylist(new MusConvPlaylistCreationRequestModel(resultKey, tracks));
				}
				catch (Exception ex)
				{
					MusConvLogger.LogFiles(ex);
				}
			}
			progressReport.Report(GetPlaylistsReportCount(result));
		}

		#endregion TransferMethods
	}
}