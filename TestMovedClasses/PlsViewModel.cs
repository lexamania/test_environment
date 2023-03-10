using MusConv.Abstractions;
using MusConv.Sentry;
using MusConv.Sentry.Extensions;
using MusConv.ViewModels.Collections.SearchResult;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Settings;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class PlsViewModel : FileManagerViewModelBase
	{
		#region Constructors

		public PlsViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Pls";
			SourceType = DataSource.Pls;
			LogoKey = LogoStyleKey.PlsLogo;
			SideLogoKey = LeftSideBarLogoKey.PlsSideLogo;
			Model = new PlsModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new ImportCsvFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			#region TransferTasks

			var transfer = new List<TaskBase_TaskItem>()
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m),
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m),
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m),
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};

			#endregion TransferTasks

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon,
				transfer, commandPlaylistsTab,
				new Initial_TaskItem("Load CSV", Initial_Update_Playlists), commandTracks);

			Tabs.Add(playlistsTab);
		}

		#endregion Constructors

		#region Commands

		public override async Task ImportFileCommand(object obj, CommandTaskType commandTaskType)
		{
			SelectedTab.Loading = true;
			SelectedTab.LoadingText = MusConvConfig.PlayListLoading;

			try
			{
				List<string> plsFilePaths = new();

				if (SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.SelectFolderInsteadOfMultipleFiles))
				{
					var filter = @$"(?i)^.+(.PLS)$";

					(_, plsFilePaths) = await GetFilesFromFolder($"Please select folder with your PLS files", filter);
				}
				else
				{
					(_, plsFilePaths) = await GetFilesFromFolder("", $"Please select PLS files", _supportedFormats);
				}

				if (plsFilePaths is null)
				{
					SelectedTab.Loading = false;
					return;
				}

				var validFiles = new List<string>();
				var corruptedFiles = new List<Tuple<string, string>>();
				foreach (var file in plsFilePaths)
				{
					if (!IsFileValid(file, out var error))
						corruptedFiles.Add(new Tuple<string, string>(file, error));
					else
						validFiles.Add(file);
				}

				plsFilePaths = validFiles;

				if (corruptedFiles.Count > 0)
				{
					var sb = new StringBuilder();
					sb.AppendLine("Error loading files:");
					foreach (var (file, error) in corruptedFiles)
					{
						sb.AppendLine($"File: {file} Error: {error}");
					}
					await ShowError(sb.ToString());

					if (corruptedFiles.Count == plsFilePaths.Count)
					{
						return;
					}
				}

				foreach (var item in plsFilePaths)
				{
					_manager.AddPath(item, _manager.GetPath(LwTabStyleKey.ItemStylePlaylist));
				}

				await InitialUpdateForCurrentTab(true);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				Initial_Setup();
			}
		}

		#endregion Commands

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] parameters)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			var playlists = parameters[0] as List<MusConvPlayList>;

			try
			{
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
					await ShowWarning(Texts.DidntMatchSongs);
					return;
				}

				var playlistsToSave = localPlaylists.Select(p => new MusConvPlayList(p.Title, _mapper.TrackMapper.MapTracks(p.Tracks))).ToList();

				await SavePlaylistsAsPls(playlistsToSave, folderPath);
			});
		}

		private async Task TransferWithoutMatching(List<MusConvPlayList> playlists)
		{
			OpenFolderDialogWrapper.InnerFileDialog.Title = Texts.SelectFolderToSave;
			var folderPath = await OpenFolderDialogWrapper.ShowAsync();

			if (string.IsNullOrWhiteSpace(folderPath))
				return;

			await SavePlaylistsAsPls(playlists, folderPath);
		}

        private async Task SavePlaylistsAsPls(List<MusConvPlayList> playlists, string folderPath)
        {
			for (var i = 0; i < playlists.Count; i++)
			{
				var currentPlaylist = playlists[i];
				
				var plsPlaylistPath = FileCreationManager.ConfigureFilePath(currentPlaylist.Title, folderPath, "pls");
				using (var sw = new StreamWriter(File.Open(plsPlaylistPath, FileMode.OpenOrCreate), Encoding.UTF8))
				{
					sw.WriteLine(PlsFileParser.ToText(currentPlaylist));
				}

				var currentPlaylistTracksToTransfer = currentPlaylist.AllTracks.Where(t => !string.IsNullOrWhiteSpace(t.Path));

				//transfer report
				var searchItems = currentPlaylistTracksToTransfer
					.Select(result => new MusConvTrackSearchResult(result, new List<MusConvTrack> { result }))
					.ToList();

				var failItems = currentPlaylist.AllTracks.Where(t => string.IsNullOrWhiteSpace(t.Path))
					.Select(result => new MusConvTrackSearchResult(result, new List<MusConvTrack> { result }))
					.ToList();

				MainViewModel.ResultVM.ResultSearchOfTracksItems.Add(new SearchResultItem<MusConvPlayList, List<MusConvTrackSearchResult>>(playlists[i], searchItems));
				(MainViewModel.TaskQItems.FirstOrDefault() as PlaylistTransfer_TaskItem).PlayListFaillItems[currentPlaylist] = failItems;
			}

			await ShowWarning(GetEndTransferMessage(folderPath));
		}

		#endregion InnerMethods
	}
}