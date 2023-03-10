using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.MessageBoxManager.Texts;
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
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class WindowsMediaPlayerViewModel : SectionViewModelBase
	{
		#region Constructors

		public WindowsMediaPlayerViewModel(MainViewModelBase main) : base(main)
		{
			Title = "Windows Media Player (WMP)";
			RegState = RegistrationState.Needless;
			SourceType = DataSource.WindowsMediaPlayer;
			LogoKey = LogoStyleKey.WindowsMediaPlayerLogo;
			SideLogoKey = LeftSideBarLogoKey.WindowsMediaPlayerSideLogo;
			Model = new WmpModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);
			
			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);
			
			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands

			#region TransferTasks

			var transferPlaylist = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, null, null, main)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(main, LwTabIconKey.PlaylistIcon, 
				transferPlaylist, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks);
			
			var albumsTab = new AlbumTabViewModelBase(main, LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks);

			var artistsTab = new ArtistTabViewModelBase(main, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks);

			#endregion Tabs
			
			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task SelectServiceAsync()
		{
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			await InitialUpdateForCurrentTab();
		}

		#endregion AuthMethods

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			return ShowHelp(MessageBoxText.WindowsMediaPlayerHelp);
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

				await SavePlaylistsAsWPL(playlistsToSave, folderPath);
			});
		}

		private async Task TransferWithoutMatching(List<MusConvPlayList> playlists)
		{
			OpenFolderDialogWrapper.InnerFileDialog.Title = Texts.SelectFolderToSave;
			var folderPath = await OpenFolderDialogWrapper.ShowAsync();

			if (string.IsNullOrWhiteSpace(folderPath))
				return;

			await SavePlaylistsAsWPL(playlists, folderPath);
		}

		private async Task SavePlaylistsAsWPL(List<MusConvPlayList> playlists, string folderPath)
		{
			for (var i = 0; i < playlists.Count; i++)
			{
				var currentPlaylist = playlists[i];

				var wplPlaylistPath = FileCreationManager.ConfigureFilePath(currentPlaylist.Title, folderPath, "wpl");

				var currentPlaylistTracksToTransfer = currentPlaylist.AllTracks
					.Where(t => !string.IsNullOrWhiteSpace(t.Path) && Path.HasExtension(t.Path))
					.ToList();
				var playlistToTransfer = currentPlaylist.Copy();
				playlistToTransfer.AllTracks = currentPlaylistTracksToTransfer;

				await using (var sw = new StreamWriter(File.Open(wplPlaylistPath, FileMode.OpenOrCreate), Encoding.UTF8))
				{
					await sw.WriteLineAsync(WplParser.ToText(playlistToTransfer));
				}

				//transfer report
				var searchItems = currentPlaylistTracksToTransfer
					.Select(result => new MusConvTrackSearchResult(result, new List<MusConvTrack> { result }))
					.ToList();

				var failItems = currentPlaylist.AllTracks.Where(t => string.IsNullOrWhiteSpace(t.Path) || !Path.HasExtension(t.Path))
					.Select(result => new MusConvTrackSearchResult(result, new List<MusConvTrack> { result }))
					.ToList();

				MainViewModel.ResultVM.ResultSearchOfTracksItems.Add(new SearchResultItem<MusConvPlayList, List<MusConvTrackSearchResult>>(currentPlaylist, searchItems));
				(MainViewModel.TaskQItems.FirstOrDefault() as PlaylistTransfer_TaskItem).PlayListFaillItems[currentPlaylist] = failItems;
			}

			var message = $"You can find WPL files in folder: {folderPath}";
			await ShowWarning($"WPL files were created. {Environment.NewLine}{message}");
		}

		#endregion InnerMethods
	}
}