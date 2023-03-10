using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Helper;
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
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Collections.SearchResult;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[FileServiceAttribute]
	public class ZplViewModel : SectionViewModelBase
	{
		#region Constructors

		public ZplViewModel(MainViewModel main) : base(main)
		{
			Title = "Zpl( Zune )";
			SourceType = DataSource.Zpl;
			LogoKey = LogoStyleKey.ZplLogo;
			SideLogoKey = LeftSideBarLogoKey.ZplSideLogo;
			RegState = RegistrationState.Needless;

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			var transferPlaylist = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, null, null, main)
			};

			var playlistTab = new PlaylistTabViewModelBase(main, LwTabIconKey.PlaylistIcon, 
				transferPlaylist, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlist), commandTracks);

			Tabs.Add(playlistTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task SelectServiceAsync()
		{
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			await Initial_Update_Playlist().ConfigureAwait(false);
		}

		#endregion AuthMethods

		#region InitialUpdate

		private async Task<bool> Initial_Update_Playlist(bool forceUpdate = false)
		{
			if (SelectedTab.MediaItems.Count == 0 || forceUpdate)
			{
				await Dispatcher.UIThread.InvokeAsync(() =>
				{
					SelectedTab.MediaItems.Clear();
				});

				SelectedTab.LoadingText = MusConvConfig.PlayListLoading;
				SelectedTab.Loading = true;

				try
				{
					List<string> filePaths = new();

					if (SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.SelectFolderInsteadOfMultipleFiles))
					{
						var filter = @"(?i)^.+(.ZPL)$";

						(_, filePaths) = await GetFilesFromFolder("Please select folder with your ZPL files", filter);
					}
					else
					{
						(_, filePaths) = await GetFilesFromFolder("", "Please select ZPL files", new List<string> { "zpl" });
					}

					if (filePaths is null)
					{
						return true;
					}

					foreach (var path in filePaths)
					{
						await OpenFile(path);
					}
				}
				catch (Exception ex)
				{
					MusConvLogger.LogFiles(ex);
					await ShowError(Texts.CantLoadFiles);
				}
				finally
				{
					Initial_Setup();
				}
			}

			return true;
		}

		#endregion InitialUpdate

		#region OpeningMethods

		public async Task OpenFile(string path)
		{
			try
			{
				var playlist = ZplParser.Parse(path);

				if (playlist is not null)
				{
					await Dispatcher.UIThread.InvokeAsync(() =>
					{
						SelectedTab.MediaItems.Add(playlist);
					});
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}
		}

		#endregion OpeningMethods

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

				await SavePlaylistsAsZPL(playlistsToSave, folderPath);
			});
		}

		private async Task TransferWithoutMatching(List<MusConvPlayList> playlists)
		{
			OpenFolderDialogWrapper.InnerFileDialog.Title = Texts.SelectFolderToSave;
			var folderPath = await OpenFolderDialogWrapper.ShowAsync();

			if (string.IsNullOrWhiteSpace(folderPath))
				return;

			await SavePlaylistsAsZPL(playlists, folderPath);
		}

		private async Task SavePlaylistsAsZPL(List<MusConvPlayList> playlists, string folderPath)
		{
			for (var i = 0; i < playlists.Count; i++)
			{
				var currentPlaylist = playlists[i];
				
				var zplPlaylistPath = FileCreationManager.ConfigureFilePath(currentPlaylist.Title, folderPath, "zpl");
				using (var sw = new StreamWriter(File.Open(zplPlaylistPath, FileMode.OpenOrCreate), Encoding.UTF8))
				{
					sw.WriteLine(ZplParser.ToText(currentPlaylist));
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

			var message = $"You can find ZPL files in folder: {folderPath}";
			await ShowWarning($"ZPL files were created. {Environment.NewLine}{message}");
		}

		#endregion InnerMethods
	}
}