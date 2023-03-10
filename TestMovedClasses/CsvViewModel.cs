using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.MessageBoxManager.Enums;
using MusConv.MessageBoxManager.Texts;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Settings;
using MusConv.ViewModels.ViewModels.Base;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.Api;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class CsvViewModel : FileManagerViewModelBase
	{
		#region Fields

		private readonly string FailedToCreateCSV =
			"Failed to create CSV file." + Environment.NewLine + "Try to create CSV file in another non-system folder.";

		#endregion Fields

		#region Constructors

		public CsvViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Csv";
			SourceType = DataSource.Csv;
			LogoKey = LogoStyleKey.CsvLogo;
			SideLogoKey = LeftSideBarLogoKey.CsvSideLogo;
			Model = new CsvModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new ImportCsvFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};
			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new ImportCsvFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};
			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new ImportCsvFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};
			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new ImportCsvFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
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

			#region Tabs

			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon,
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase);
			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon,
				transfer, commandPlaylistsTab,
				new Initial_TaskItem("Load CSV", Initial_Update_Playlists), commandTracks);
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				transfer, commandAlbumsTab,
				new Initial_TaskItem("Load CSV", Initial_Update_Album), commandTracks);
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon,
				transfer, commandArtistsTab,
				new Initial_TaskItem("Load CSV", Initial_Update_Artists), commandTracks);

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			ShowHelp(MessageBoxText.CsvHelp);
			return Task.CompletedTask;
		}

		#endregion Commands

		#region CommandsConfirms

		public override async Task ConfirmDelete_Click(object obj)
		{
			await base.ConfirmDelete_Click(obj).ConfigureAwait(false);
			await InitialUpdateForCurrentTab(true);
		}

		#endregion CommandsConfirms

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			string type = string.Empty;
			string message = string.Empty;
			string saveName = string.Empty;
			string savePath = string.Empty;
			
			var count = 0;
			try
			{
				var playListUiItems = (IEnumerable<MusConvModelBase>) items[0];
				if (items[0] is List<MusConvPlayList> playlsit)
				{
					if (playlsit.Count == 1 && playlsit[0].Title == "Favorites")
					{
						type = "Tracks";
						count = playlsit.SelectMany(x => x.AllTracks).Count();
					}
					else
					{
						type = "Playlists";
						count = playlsit.Count;
					}
				}
				else if (items[0] is IList<MusConvArtist> ar)
				{
					type = "Artists";
					count = ar.Count;
				}
				else if (items[0] is IList<MusConvAlbum> al)
				{
					type = "Albums";
					count = al.Count;
				}

				if (items != null && items.Length == 2)
				{
					message = items[1].ToString();
					saveName = message.Replace("CSV file ", "").Replace(" with not found songs was created.", "");
				}
				else if (count > 1)
				{
					saveName = $"{count}{type}From{MainViewModel.SelectedItem}";
				}
				else
				{
					saveName = $"{count}{type.Substring(0, type.Length - 1)}From{MainViewModel.SelectedItem}";
				}

				await Dispatcher.UIThread.InvokeAsync(async () =>
				{
					var csvWasCreated = false;
					if (SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.CSVToSingleFile))
					{
						SaveFileDialogWrapper.InnerFileDialog.InitialFileName = saveName;
						SaveFileDialogWrapper.InnerFileDialog.Filters = new List<Avalonia.Controls.FileDialogFilter>
						{
							new Avalonia.Controls.FileDialogFilter
							{
								Name = "",
								Extensions = new List<string> {"csv"},
							}
						};

						savePath = await SaveFileDialogWrapper.ShowAsync();

						if (savePath != null)
						{
							saveName = Path.GetFileNameWithoutExtension(savePath);
							csvWasCreated = await SaveToCsvFileAsync(playListUiItems, savePath);
							message = $"CSV file {saveName}.csv  was created.";
						}
					}
					else
					{
						savePath = await OpenFolderDialogWrapper.ShowAsync();
						if (savePath != null)
						{
							foreach (var item in playListUiItems.ToList())
							{
								csvWasCreated = await SaveToCsvFileAsync(new List<object> {item}, Path.Combine(savePath,
									string.Join("_",
										string.Join("_", item.Title).Split(Path.GetInvalidFileNameChars())))
								);
							}

							if (playListUiItems.Count() == 1)
							{
								message = $"1 CSV file was created at {savePath}";
							}
							else
							{
								message = $"{playListUiItems.Count()} CSV files were created at {savePath}";
							}
						}
					}

					if (csvWasCreated)
					{
						await ShowMessage(message, Icon.Info);
					}
				});
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				await ShowError(FailedToCreateCSV);
			}
		}

		#endregion TransferMethods

		#region InnerMethods

		public async Task<bool> SaveToCsvFileAsync(IEnumerable<object> entries, string filename)
		{
			if (!entries.Any())
			{
				return true;
			}

			var trackCounter = 0;
			var csv = new StringBuilder();
			var header = new List<string> {"Track", "Artist", "Album", "Duration", "Explicit", "Genre", "Year", "Url"};

			if (entries is IEnumerable<MusConvArtist>)
			{
				header = new List<string> {"Artist"};
			}
			else if (entries is IEnumerable<MusConvTrack>)
            {
				header.Add("ISRC");
            }

			csv.AppendLine(string.Join(@",", header));
			try
			{
				foreach (var entry in entries)
				{
					if (entry is MusConvArtist artist)
					{
						csv.AppendLine(artist.Title?.ReplaceAll(",", " ") ?? "");
					}

					if (entry is MusConvTrack track)
					{
						csv.AppendLine(string.Join(@",", 
							track.Title?.ReplaceAll(",", " ") ?? "",
							track.Artist?.ReplaceAll(",", " ") ?? "",
							track.Album?.ReplaceAll(",", " ") ?? "",
							track.Duration,
							track.IsExplicit,
							track.Genre?.ReplaceAll(",", " &") ?? "",
							track.Year ?? "",
							track.Url ?? "",
							track.ISRC ?? ""));

						if (MusConvLogin.IsTrial && !TrackerManager.TransferTracker.TryIncrementHowMuchTodayTransferred())
						{
							await TrialPeriod(entries);
							break;
						}
					}
					else
					{
						foreach (var item in (entry as MusConvWithTracksBase)?.AllTracks)
						{
							csv.AppendLine(string.Join(@",",
								item.Title?.ReplaceAll(",", " ") ?? "",
								item.Artist?.ReplaceAll(",", " ") ?? "",
								item.Album?.ReplaceAll(",", " ") ?? "",
								item.Duration,
								item.IsExplicit,
								item.Genre?.ReplaceAll(",", " &") ?? "",
								item.Year ?? "",
								item.Url ?? ""));

							if (MusConvLogin.IsTrial && !TrackerManager.TransferTracker.TryIncrementHowMuchTodayTransferred())
							{
								await TrialPeriod(entries);
								break;
							}
						}
					}
				}

				await File.WriteAllTextAsync(GetNextFilename(filename), csv.ToString(),
					Encoding.UTF8);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				await ShowError(FailedToCreateCSV);
				return false;
			}

			return true;
		}

		public static string GetNextFilename(string filename)
		{
			int i = 1;
			string dir = Path.GetDirectoryName(filename);
			string file = Path.GetFileNameWithoutExtension(filename) + "{0}";
			string extension = Path.GetExtension(filename);
			if (string.IsNullOrEmpty(extension))
			{
				extension = ".csv";
				filename += extension;
			}

			while (File.Exists(filename))
				filename = Path.Combine(dir, string.Format(file, "(" + i++ + ")") + extension);

			return filename;
		}

		#endregion InnerMethods
	}
}