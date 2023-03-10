using ATL;
using ATL.Playlist;
using MediaBrowser.Model.Extensions;
using MusConv.Abstractions;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Settings;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusConv.Sentry.Extensions;
using MusConv.ViewModels.Collections.SearchResult;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class RekordboxViewModel : FileManagerViewModelBase
	{
		#region Constructors

		public RekordboxViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Rekordbox";

			SourceType = DataSource.Rekordbox;
			LogoKey = LogoStyleKey.RekordboxLogo;
			SideLogoKey = LeftSideBarLogoKey.RekordboxSideLogo;
			BaseUrl = "https://rekordbox.com/en/";
			Model = new RekordboxModel();
			IsSuitableForAutoSync = false;

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ImportXmlFileCommand(ImportFileCommand,CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar)
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ImportXmlFileCommand(ImportFileCommand,CommandTaskType.CommandBar),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ImportXmlFileCommand(ImportFileCommand,CommandTaskType.CommandBar),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ImportXmlFileCommand(ImportFileCommand,CommandTaskType.CommandBar)
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			#region TransferTasks

			var transfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, null, null, m),
				/*
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, null, null, m),
				new AlbumTransferTaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, null, null),
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, null, null)
				*/
			};

			#endregion TransferTasks

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				transfer, commandPlaylistsTab, 
				new Initial_TaskItem("Load XML", Initial_Update_Playlists), commandTracks);
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				transfer, commandAlbumsTab, 
				new Initial_TaskItem("Load XML", Initial_Update_Album), commandTracks);
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				transfer, commandArtistsTab, 
				new Initial_TaskItem("Load XML", Initial_Update_Artists), commandTracks);
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab, 
				new Initial_TaskItem("Load XML", Initial_Update_Tracks), EmptyCommandsBase);

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] parameters)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			
			try
			{
                var playlists = parameters[0] as List<MusConvPlayList>;

                if (!SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.ScanLocalFolder) && playlists.Any(p => p.AllTracks.Any(t => !string.IsNullOrWhiteSpace(t?.Path))))
				{
					await TransferWithoutMatching(playlists);
				}
				else
				{
					await TransferWithMatching(playlists);
				}

				MainViewModel.ResultVM.WasFirstTransfer = true;
				IsResultsWindowAlreadyShowed = false;
				await ShowErrorsAsync();
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
			}
		}

		#endregion TransferMethods

		#region InnerMethods

		private async Task TransferWithMatching(List<MusConvPlayList> playlists)
		{
			var filter =
					 @"(?i)^.+(.AAC|.MP4|.M4A|.AIF|.AIFF|.AIFC|.DSD|.DSF|.AC3|.FLAC|.GYM|.MID|.MIDI|.APE|.MP1|.MP2|.MP3|.MPC|.MP|.MOD|.OGG|.OPUS|.OFR|.OFS|.PSF|.PSF1|.PSF2|.MINIPSF|.MINIPSF1|.MINIPSF2|.SSF|.MINISSF|.DSF|.MINIDSF|.GSF|.MINIGSF|.QSF|.MINISQF|.S3M|.SPC|.TAK|.VQF|.WAV|.BWAV|.BWF|.VGM|.VGZ|.WV|.WMA|.ASF|.M4P)$";

			(string folderPath, List<string> filePaths) = await GetFilesFromFolder(Texts.SelectFolder, filter);

			if (folderPath is null || filePaths is null)
				return;

			await Task.Run(async () =>
			{
				var localPlaylists = MatchPlaylistsWithLocalMedias(playlists, filePaths, true);

				if (!localPlaylists.Any(playlist => playlist.Tracks.Any(track => track.SimilarLocalTracks.Count > 0)))
				{
					await ShowWarning(Texts.DidntMatchSongsForM3U);
					return;
				}

				var playlistsToSave = localPlaylists.Select(p => new MusConvPlayList(p.Title, _mapper.TrackMapper.MapTracks(p.Tracks))).ToList();

				await SavePlaylistsAsM3U8(playlistsToSave, folderPath);
			});
		}

		private async Task TransferWithoutMatching(List<MusConvPlayList> playlists)
		{
			OpenFolderDialogWrapper.InnerFileDialog.Title = Texts.SelectFolderToSave;
			var folderPath = await OpenFolderDialogWrapper.ShowAsync();

			await SavePlaylistsAsM3U8(playlists, folderPath);
		}

		private async Task SavePlaylistsAsM3U8(List<MusConvPlayList> playlists, string folderPath)
		{
			if (string.IsNullOrWhiteSpace(folderPath))
				return;
			for (var i = 0; i < playlists.Count; i++)
			{
				var currentPlaylist = playlists[i];
				
				var m3u8PlaylistPath = FileCreationManager.ConfigureFilePath(currentPlaylist.Title, folderPath, "m3u8");
				using (var sw = new StreamWriter(File.Open(m3u8PlaylistPath, FileMode.OpenOrCreate), Encoding.UTF8))
				{
					sw.WriteLine("");
				}

				var m3uPlaylist = PlaylistIOFactory.GetInstance().GetPlaylistIO(m3u8PlaylistPath);

				if (m3uPlaylist.Tracks?.Count > 0)
					m3uPlaylist.Tracks?.Clear();

				var currentPlaylistTracksToTransfer = currentPlaylist.AllTracks.Where(t => !string.IsNullOrWhiteSpace(t.Path));

				m3uPlaylist.Tracks = currentPlaylistTracksToTransfer
					.Select(t => new Track(t?.Path)
					{
						Title = t.Title,
						Artist = t.Artist,
						Album = t.Album
					})?.DistinctBy(t => new { t.Title, t.Artist, t.Album })?.ToList();

				//transfer report
				var searchItems = currentPlaylistTracksToTransfer
					.Select(result => new MusConvTrackSearchResult(result, new List<MusConvTrack> { result }))
					.ToList();

				var failItems = currentPlaylist.AllTracks.Where(t => string.IsNullOrWhiteSpace(t.Path))
					.Select(result => new MusConvTrackSearchResult(result, new List<MusConvTrack> { result }))
					.ToList();
				
				MainViewModel.ResultVM.ResultSearchOfTracksItems.Add(new SearchResultItem<MusConvPlayList, List<MusConvTrackSearchResult>>(currentPlaylist, searchItems));
				(MainViewModel.TaskQItems.FirstOrDefault() as PlaylistTransfer_TaskItem).PlayListFaillItems[currentPlaylist] = failItems;
			}

			var message = $"You can find M3U8 files in folder: {folderPath}";
			await ShowWarning($"M3U8 files were created. {Environment.NewLine}{message}{Environment.NewLine}{Environment.NewLine}"
						+ "In Rekordbox program please click \"File>Import>ImportPlaylist\" and select just created m3u8 file by MusConv");
		}

		#endregion InnerMethods
	}
}