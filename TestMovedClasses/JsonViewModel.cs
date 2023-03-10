using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.ViewModels.Base;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using Newtonsoft.Json.Linq;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class JsonViewModel : FileManagerViewModelBase
	{
		#region Constructors

		public JsonViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Json";
			CurrentVMType = VmType.FileVM;
			IsSuitableForAutoSync = false;
			SourceType = DataSource.Json;
			LogoKey = LogoStyleKey.JsonLogo;
			SideLogoKey = LeftSideBarLogoKey.JsonSideLogo;
			Model = new JsonModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ImportJsonFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase);

			#endregion Commands

			#region TransferTasks

			var transferPlaylist = new List<TaskBase_TaskItem>()
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m),
			};
			var transferTracks = new List<TaskBase_TaskItem>()
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m),
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon,
				transferPlaylist, commandPlaylistsTab,
				new Initial_TaskItem("Load JSON", Initial_Update_Playlists), commandTracks);
			
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon,
				transferTracks, commandTracksTab,
				new Initial_TaskItem("Load JSON", Initial_Update_Tracks), EmptyCommandsBase);

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			var playlists = (items[0] as List<MusConvPlayList> ?? new List<MusConvPlayList>()).ToList();

			foreach (var playlist in playlists)
			{
				await Export(playlist.AllTracks.ToList(), playlist.Title);
			}
			await ShowWarning($"JSON files were created.");
		}

		#endregion TransferMethods

		#region InnerMethods

		public async Task Export(List<MusConvTrack> tracks, string nameOfFile)
		{
			JArray body = new JArray();
			SaveFileDialogWrapper.InnerFileDialog.InitialFileName = nameOfFile + "JSON";
			SaveFileDialogWrapper.InnerFileDialog.Filters = new List<Avalonia.Controls.FileDialogFilter>
			{
				new Avalonia.Controls.FileDialogFilter
				{
					Name = "",
					Extensions = _supportedFormats,
				}
			};
			var savePath = await Dispatcher.UIThread.InvokeAsync(SaveFileDialogWrapper.ShowAsync);

			// ShowAsync returns empty string if user closes the import window
			// without selecting the destination path
			if (string.IsNullOrEmpty(savePath)) return;

			await using StreamWriter file = new(Path.Combine(savePath));
			foreach (var track in tracks)
			{
				var song = new JObject
				{
					{"title", track.Title},
					{"artist", track.Artist},
					{"album", track.Album ?? ""}
				};
				body.Add(song);
			}
			var jsonString = JsonConvert.SerializeObject(body, Formatting.Indented);
			await file.WriteAsync($"{jsonString}");
		}

		#endregion InnerMethods
	}
}