using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class MusConvViewModel : SectionViewModelBase
	{
		#region Constructors

		public MusConvViewModel(MainViewModelBase m) : base(m)
		{
			Title = "MusConv";
			CurrentVMType = VmType.Service;
			RegState = RegistrationState.Needless;
			SourceType = DataSource.MusConv;
			LogoKey = LogoStyleKey.MusConvServiceLogo;
			SideLogoKey = LeftSideBarLogoKey.MusConvServiceSideLogo;
			Model = new MusConvServiceModel();
			IsHelpVisible = false;
			IsSelfTransfer = true;
			BaseUrl = "https://musconv.com/";

			#region Commands

			var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new EditCommand(Command_EditTrack, CommandTaskType.CommandBar),
				new EditCommand(Command_EditTrack, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar)
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar)
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new EditCommand(Edit_Confirm),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
				new EditCommand(Command_Edit, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.DropDownMenu),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new EditCommand(Command_EditTrack, CommandTaskType.CommandBar),
				new EditCommand(Command_EditTrack, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
			};
			
			var artistsCommands = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar)
			};
			
			var albumCommands = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
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
			var artistTransfer = new List<TaskBase_TaskItem>()
			{
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};
			var tracksTransfer = new List<TaskBase_TaskItem>()
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandPlaylistTracks);
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, albumCommands,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks);
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon,
				artistTransfer, artistsCommands, 
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks);
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon,
				tracksTransfer, commandTracksTab, 
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase);

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			await InitialUpdateForCurrentTab().ConfigureAwait(false);

			return true;
		}

		#endregion AuthMethods

		#region SearchMethods

		public override async Task<MusConvTrackSearchResult> SearchTrackByDefaultAndByAlbumsAsync(MusConvTrack track, CancellationToken token)
		{
			return new MusConvTrackSearchResult(track, new List<MusConvTrack>{ track });
		}

		#endregion SearchMethods

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);

			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			
			await Transfer_DoWork(items[0]).ConfigureAwait(false);
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);
			
			foreach (var resultKey in result.Keys)
			{
				var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
				var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);

				progressReport.Report(new ReportCount(0, $"Adding tracks to playlist \"{resultKey}\", please wait",
					ReportType.Sending));
				MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

				var tasks = result[resultKey]
					.Select(x => x.ResultItems.FirstOrDefault())
					.ToList()
					.Select(it => Model.AddTracksToPlaylist(createdPlaylist, new List<MusConvTrack>{it}))
					.ToList();
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}

			await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(result)));
		}

		#endregion TransferMethods
	}
}