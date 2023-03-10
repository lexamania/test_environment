using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.MessageBoxManager.Texts;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[FileServiceAttribute]
	public class TxtViewModel : FileManagerViewModelBase
	{
		#region Fields

		private readonly string TXTformat =
		"Each row of txt file represents one song in following format:" + Environment.NewLine +
		"Song name - Artist name - Album name" + Environment.NewLine +
		"Album name is optional." + Environment.NewLine +
		"You can check sample TXT file here: " + Environment.NewLine +
		"www.musconv.com/playlists/songs.txt";

		#endregion Fields

		#region Constructors

		public TxtViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Txt";
			SourceType = DataSource.Txt;
			LogoKey = LogoStyleKey.TxtLogo;
			SideLogoKey = LeftSideBarLogoKey.TxtSideLogo;
			RegState = RegistrationState.Needless;
			Model = new TxtModel();
			IsSuitableForAutoSync = false;

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new ImportTxtFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
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
				new Initial_TaskItem("Load txt", Initial_Update_Playlists), commandTracks);
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				transfer, commandAlbumsTab,
				new Initial_TaskItem("Load txt", Initial_Update_Album), commandTracks);
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon,
				transfer, commandArtistsTab,
				new Initial_TaskItem("Load txt", Initial_Update_Artists), commandTracks);

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region Commands

		public override async Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			await ShowHelp(MessageBoxText.TxtHelp);
		}

		#endregion Commands

		#region CommandsConfirms

		public override async Task ConfirmDelete_Click(object obj)
		{
			SelectedTab.Loading = true;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent).ConfigureAwait(false);

			if (SelectedTab.Lwstyle == LwTabStyleKey.ItemStylePlaylist)
			{
				SelectedTab.LoadingText = MusConvConfig.PlayListLoading;
				foreach (MusConvPlayList musConvItemBase in SelectedItems)
				{
					if (Model != null)
					{
						await Model.RemovePlaylist(musConvItemBase);
					}
				}

				SelectedTab.MediaItems.RemoveRange(SelectedItems);
				MainViewModel.Message = "Playlists deleted!";
			}
			else if (SelectedTab.Lwstyle == LwTabStyleKey.ItemStyleTrack)
			{
				SelectedTab.LoadingText = MusConvConfig.SongsLoading;
				if (Model != null)
				{
					await Model.RemoveTracksFromFavorites(SelectedItems.Cast<MusConvTrack>().ToList());
				}

				SelectedTab.MediaItems.RemoveRange(SelectedItems);
				MainViewModel.Message = "Tracks deleted!";
			}
			else if (SelectedTab.Lwstyle == LwTabStyleKey.ItemStyleAlbum)
			{
				SelectedTab.LoadingText = MusConvConfig.AlbumLoading;
				if (Model != null)
				{
					await Model.RemoveAlbums(SelectedItems.Cast<MusConvAlbum>().ToList());
				}
				SelectedTab.MediaItems.RemoveRange(SelectedItems);
				MainViewModel.Message = "Albums deleted!";
			}
			else if (SelectedTab.Lwstyle == LwTabStyleKey.ItemStyleArtist)
			{
				SelectedTab.LoadingText = MusConvConfig.ArtistLoading;
				if (Model != null)
				{
					await Model.RemoveArtists(SelectedItems.Cast<MusConvArtist>().ToList());
				}
				SelectedTab.MediaItems.RemoveRange(SelectedItems);
				MainViewModel.Message = "Artists deleted!";
			}
			SelectedItems = null;
			SelectedTab.Loading = false;
		}

		#endregion CommandsConfirms

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
			{
				string savePath = await OpenFolderDialogWrapper.ShowAsync();

				// ShowAsync returns empty string if user closes the import window
				// without selecting the destination path
				if (string.IsNullOrEmpty(savePath)) return;

				var data = GetDataForWriting((IEnumerable<MusConvModelBase>)items[0]).ToList();

				List<string> filepaths = new List<string>();

				if (items[0] is List<MusConvPlayList> && (items[0] as List<MusConvPlayList>).First().Title != "Favorites")
				{
					filepaths.AddRange(GetFilePaths((items[0] as IEnumerable<MusConvModelBase>), savePath));
				}
				else
				{
					filepaths.Add(GetFilePath((items[0] as IEnumerable<MusConvModelBase>).First(), savePath));
				}

				for (int i = 0; i < filepaths.Count; ++i)
				{
					await File.WriteAllTextAsync(filepaths[i], data[i]);
				}

				string fileWord = "file";

				if (filepaths.Count > 1)
					fileWord += "s";

				await ShowMessage($"{filepaths.Count} txt {fileWord} was created");
			});			  
		}

		#endregion TransferMethods

		#region InnerMethods

		private static IEnumerable<string> GetDataForWriting(IEnumerable<MusConvModelBase> items)
		{
			var sb = new StringBuilder();
			var stringsList = new List<string>();
			
			switch (items)
			{
				case IEnumerable<MusConvAlbum>:
					foreach (MusConvAlbum album in items)
					{
						sb.AppendLine($"{album.Title} - {album.Artist}");
					}
					stringsList.Add(sb.ToString());
					break;
				case IEnumerable<MusConvPlayList>:					
					if (items.Count() == 1 && items.First().NameToSend == "Favorites")
					{
						var playlist = (MusConvPlayList)items.First();

						foreach (var track in playlist.AllTracks)
							sb.AppendLine($"{track.Title} - {track.Artist}");
						stringsList.Add(sb.ToString());
					}
					else
					{
						foreach (MusConvPlayList playlist in items)
						{
							foreach(var track in playlist.AllTracks)
							{
								sb.AppendLine($"{track.Title} - {track.Artist} - {track.Album}");
							}
							stringsList.Add(sb.ToString());
							sb.Clear();
						}						   
					}				 
					break;
				case IEnumerable<MusConvArtist>:
					foreach (MusConvArtist artist in items)
					{
						sb.AppendLine($"{artist.Title}");
					}
					stringsList.Add(sb.ToString());
					break;
				default:
					throw new Exception($"Type {items.GetType()} is not supported");
			}
			return stringsList;
		}

		private static string GetFilePath(MusConvModelBase item, string directoryPath)
		{
			var filename = $"MusConvTxt{GetFilename(item)}";
			var existedFiles = Directory.GetFiles(directoryPath, $"{filename}*");

			string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
			Regex r = new Regex(string.Format($"[{Regex.Escape(regexSearch)}]"));
			filename = r.Replace(filename, "");

			return Path.Combine(directoryPath, filename) + $"{GetNumberOfExistedFile(existedFiles)}.txt";
		}

		private static List<string> GetFilePaths(IEnumerable<MusConvModelBase> items, string directoryPath)
		{
			var paths = new List<string>();
			string filename;

			foreach(var item in items)
			{
				filename = GetFilename(item);

				string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
				Regex r = new Regex(string.Format($"[{Regex.Escape(regexSearch)}]"));
				filename = r.Replace(filename, "");

				var existedFiles = Directory.GetFiles(directoryPath, $"{filename}*");			  

				var duplicatedFiles = paths.Where(p => Path.GetFileName(p).StartsWith(filename));
				paths.Add(Path.Combine(directoryPath, filename) + $"{GetNumberOfExistedFile(existedFiles.Concat(duplicatedFiles).ToArray())}.txt");

			}
			return paths; 
		}

		private static string GetFilename(MusConvModelBase item)
		{		   
			if(item.Title == "Favorites")
				return "Tracks";

			return item switch
			{
				MusConvPlayList => item.Title,
				MusConvAlbum => "Albums",
				MusConvArtist => "Artists",
				_ => throw new ArgumentException($"Type {item.GetType()} is not supported")
			};
		}

		private static string GetNumberOfExistedFile(string[] existedFiles)
		{
			if (!existedFiles.Any())
				return "";

			var number = "(1)";
			var reg = new Regex(@"(\d+\)\.txt)");
			var maxNumber = existedFiles.Select(name => Convert.ToInt16(
				reg.Matches(name).FirstOrDefault()?.Value.Replace(").txt", ""))).Max();
			if (maxNumber != 0) number = $"({++maxNumber})";

			return number;
		}

		#endregion InnerMethods
	}
}