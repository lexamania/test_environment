using Avalonia.Threading;
using MusConv.MessageBoxManager.Enums;
using MusConv.MessageBoxManager.Texts;
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
using System.Threading.Tasks;
using System.Xml.Linq;
using MusConv.Abstractions;
using MusConv.Lib.Apple;
using MusConv.Lib.Apple.Models;
using MusConv.Sentry.Extensions;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Export.Base;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Abstractions.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	// TODO: move common logic to ItunesModel to separate logic (CreatePlaylist, Empty, Remove, etc.)
	public class ItunesViewModel : XmlViewModelBase
	{
		#region Fields

		private ItunesServiceManager ItunesManager => _manager as ItunesServiceManager;
		public bool IsForceUpdate { get; set; } = false;
		public static Action ActionCommand;
		public static bool IsWorker_DoWorkAsync = true;
		public static bool Flag = true;
		private const string SendFileForReview = "Please send .xml/txt file to support@musconv.com for review.";

		#endregion Fields

		#region Constructors

		public ItunesViewModel(MainViewModelBase m) : base(m)
		{
			Title = "iTunes";
			Model = new ItunesModel();
			(Model as ItunesModel).Manager.Parser.OnParsedItemChanged += (name) => SelectedTab.LoadingText = $"Loading {name}";
			SourceType = DataSource.ITunes;
			LogoKey = LogoStyleKey.ItunesLogo;
			SmallLogoKey = LogoStyleKey.ItunesLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.ItunesSideLogo;
			IsSuitableForAutoSync = false;
			BaseUrl = "https://www.apple.com/itunes/";

			#region Commands
			
			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ImportItunesFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_Duplicate, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_Duplicate, CommandTaskType.DropDownMenu),
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
				new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.DropDownMenu),
				new ExportItunesFileCommand(Command_ItunesExport, CommandTaskType.DropDownMenu),
				new ExportItunesFileCommand(Command_ItunesExport, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
			};
			
			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ImportItunesFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
			};
			
			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ImportItunesFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnAppleMusicCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
                new EditCommand(Command_EditTrack, CommandTaskType.DropDownMenu),
				new EditCommand(Command_EditTrack, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
			};

			#endregion Commands

			#region TransferTasks

			var transfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, null, null, m),
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, null, null, m),
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, null, null, m),
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, null, null, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				transfer, commandPlaylistsTab, 
				new Initial_TaskItem("Load xml/txt", Initial_Update_Playlists), commandTracks);
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				transfer, commandAlbumsTab,
				new Initial_TaskItem("Load xml/txt", Initial_Update_Album), commandTracks);
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				transfer, commandArtistsTab, 
				new Initial_TaskItem("Load xml/txt", Initial_Update_Artists), commandTracks);

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
		}

		#endregion Constructors

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			ShowHelp(MessageBoxText.iTunesHelp);

			return Task.CompletedTask;
		}

		private async Task Command_ItunesExport(object arg, CommandTaskType type)
		{
			var playlists = type == CommandTaskType.CommandBar 
				? SelectedTab.MediaItems.Where(x => x.IsSelected).Cast<MusConvPlayList>().ToList()
				: new List<MusConvPlayList> { arg as MusConvPlayList };

			if (playlists.Count == 0)
			{
				await ShowError(Texts.NoPlaylistsSelected).ConfigureAwait(false);
				return;
			}

			OpenFolderDialogWrapper.InnerFileDialog.Title = Texts.SelectFolderToSave;
			var folderPath = await OpenFolderDialogWrapper.ShowAsync();

			SelectedTab.LoadingText = "Exporting playlists...";
			SelectedTab.Loading = true;
			var xDocument = new XDocument(new XDeclaration("1.0", "UTF-8", null), new XDocumentType("plist", "-//Apple Computer//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null));
			var splitPlaylists = SplitPlaylists(playlists);
			xDocument.Add(await CreatePlist(splitPlaylists).ConfigureAwait(false));

			var fileName = Path.GetFileNameWithoutExtension(splitPlaylists.Keys.First());
			var existedFiles = Directory.GetFiles(folderPath, $"{fileName}*");
			var xDocName = $"{fileName}{ItunesManager.GetNumberOfExistedFile(existedFiles)}.xml";
			var savePath = Path.Combine(folderPath, xDocName);
			xDocument.Save(savePath);
			ItunesManager.FixXml(savePath);

			SelectedTab.Loading = false;
			await ShowMessage($"Playlists successfully exported to {savePath} file.").ConfigureAwait(false);

			// local functions

			static Dictionary<string, List<MusConvPlayList>> SplitPlaylists(IEnumerable<MusConvPlayList> playlists)
			{
				Dictionary<string, List<MusConvPlayList>> splitPlaylists = new ();
				foreach (var playlist in playlists)
				{
					if (splitPlaylists.TryGetValue(playlist.Description, out var pls))
						pls.Add(playlist);
					else
						splitPlaylists.Add(playlist.Description, new List<MusConvPlayList> {playlist});
				}

				return splitPlaylists;
			}

			async Task<XElement> CreatePlist(Dictionary<string, List<MusConvPlayList>> splitPlaylists)
			{
				var xmlTracks = new List<XElement>();
				var xmlPlaylists = new List<XElement>();
				foreach (var (path, playlists) in splitPlaylists)
				{
					var xdoc = await ((ItunesModel) Model).LoadXmlDocument(path).ConfigureAwait(false);
					foreach (var playlist in playlists)
					{
						var trackKeys = xdoc.Element("plist")
							.Element("dict")
							.Element("dict")
							.Elements("key")
							.Where(x => playlist.AllTracks.Select(y => y.Id).Contains(x.Value));

						foreach (var key in trackKeys)
						{
							xmlTracks.Add(key);
							// next node is the track data by key
							xmlTracks.Add(key.NextNode as XElement);
						}

						xmlPlaylists.Add(GetPlaylistFromPlist(xdoc, playlist.Id).Playlist);
					}
				}

				return ItunesManager.GetPlist(xmlTracks, xmlPlaylists);
			}
		}

		public async Task Command_Duplicate(object arg, CommandTaskType type)
		{
			List<MusConvPlayList> deleteItems;
			if (type == CommandTaskType.CommandBar)
			{
				deleteItems = SelectedTab.MediaItems.Where(x => x.IsSelected).Cast<MusConvPlayList>().ToList();
			}
			else
			{
				deleteItems = new List<MusConvPlayList>() { arg as MusConvPlayList };
			}

			var res = await ShowMessage(Texts.DeleteDublicateTracks, ButtonEnum.YesNo, Icon.Warning).ConfigureAwait(false);
			if (res != ButtonResult.Yes)
			{
				return;
			}

			if (deleteItems == null || deleteItems.Count < 1)
			{
				await ShowError(Texts.NoPlaylistsSelected);
				return;
			}

			SelectedTab.LoadingText = MusConvConfig.PlayListLoading;
			SelectedTab.Loading = true;
			var playlists = new Dictionary<MusConvPlayList, IEnumerable<MusConvTrack>>();
			foreach (MusConvPlayList playList in deleteItems)
			{
				var duplicateItems = playList.AllTracks.GroupBy(x => x.FullName.ToLower()).Where(x => x.Count() > 1)
							   .SelectMany(x => x.ToList().GetRange(1, x.Count() - 1));
				if (duplicateItems != null && duplicateItems.Any())
				{
					playlists.Add(playList, duplicateItems.ToList());
				}
			}

			foreach (var playlist in playlists)
			{
				DeleteTracksFromPlaylist(playlist.Key, playlist.Value);
			}
			SelectedTab.Loading = false;
			await ShowMessage(Texts.DublicateTracksDeleted, Icon.Success);
		}

		#endregion Commands

		#region CommandsConfirms

		public override async Task ConfirmDelete_Click(object obj)
		{
			SelectedTab.Loading = true;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent).ConfigureAwait(false);
			
			foreach (var musConvItemBase in SelectedItems)
				SelectedTab.MediaItems.Remove(musConvItemBase);
			
			DeleteItemsFromSourceFile(SelectedItems);
			SelectedItems = null;
			SelectedTab.Loading = false;
		}

		public override async Task ConfirmClone_Click(object obj)
		{
			SelectedTab.Loading = true;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			if (SelectedItems.All(x => (x as MusConvPlayList).AllTracks.Count == 0))
			{
				var message = (SelectedItems.Count > 1) ? "playlists are" : "playlist is";
				await ShowError($"Selected {message} empty");
				SelectedTab.Loading = false;
				return;
			}
			foreach (MusConvPlayList playList in SelectedItems)
			{
				if (playList.AllTracks != null && playList.AllTracks.Count > 0)
				{
					CloneLocalPlaylist(playList);
					AddItemsToSourceFile(SelectedItems);
				}
			}
			SelectedTab.Loading = false;
			MainViewModel.Message = (SelectedItems.Count > 1) ? "Playlists cloned!" : "Playlist cloned!";
			SelectedItems = null;
		}

		public override async Task ConfirmEmpty_Click(object obj)
		{
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

            if (SelectedItems.All(x => (x as MusConvPlayList).AllTracks.Count == 0))
			{
				var message = (SelectedItems.Count > 1) ? "playlists are" : "playlist is";
				await ShowError($"Selected {message} already empty");
				return;
			}

            var emptyWithDescription = SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.EmptyPlaylistsWithDescription);
			
            foreach (MusConvPlayList playList in SelectedItems)
			{
				if (playList.AllTracks != null && playList.AllTracks.Count > 0)
				{
					if (emptyWithDescription)
					{
						playList.Description = string.Empty;
					}

                    EmptyLocalPlayList(playList);
				}
			}
			EmptyPlaylists(SelectedItems);
			await InitialUpdateForCurrentTab(true);
			MainViewModel.Message = (SelectedItems.Count > 1) ? "Playlists emptied!" : "Playlist emptied!";
			SelectedItems = null;
		}

		public override async Task ConfirmRemoveFromPlaylist_Click(object obj)
		{
			SelectedTab.Loading = true;
			foreach (var track in SelectedTracks)
			{
				SelectedTab.SelectedMediaItem.AllTracks.Remove(track);
			}
			DeleteTracksFromPlaylist(SelectedTab.SelectedMediaItem as MusConvPlayList, SelectedTracks);
			SelectedTab.Loading = false;
			MainViewModel.Message = (SelectedTracks.Count > 1) ? "Tracks deleted!" : "Track deleted!";
			SelectedTracks = null;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			NavigateToTracks();
		}

		public override async Task ConfirmAddToPlaylist(object obj)
		{
			SelectedTab.Loading = true;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			try
			{
				AddTracksToPlaylist(TargetPlaylist, SelectedTracks);
				TargetPlaylist.AllTracks.AddRange(SelectedTracks);
				MainViewModel.Message = (SelectedTracks.Count > 1) ? "Tracks added!" : "Track added!";
				Tabs.FirstOrDefault().Search = string.Empty;
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
			}
			TargetPlaylist = null;
			SelectedTracks = null;
			SelectedTab.Loading = false;
			if (SelectedItems == null) NavigateToTracks();
			SelectedItems = null;
		}

		public override async Task ConfirmCreatePlaylist(object obj)
		{
			SelectedTab.Loading = true;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			if (string.IsNullOrEmpty(TargetPlaylist.Title))
			{
				await ShowWarning("Title can't be empty");
				SelectedTab.Loading = false;
				return;
			}
			try
			{
				AddPlaylistToSourceFile(TargetPlaylist);
				SelectedTab.MediaItems.Add(TargetPlaylist);
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
			}
			SelectedTab.Loading = false;
			TargetPlaylist = null;
		}

		public override async Task ConfirmReplaceInPlaylist(object obj)
		{
			SelectedTab.Loading = true;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			try
			{
				EmptyPlaylist(TargetPlaylist);
				EmptyLocalPlayList(TargetPlaylist);
				AddTracksToPlaylist(TargetPlaylist, SelectedTracks);
				TargetPlaylist.AllTracks.AddRange(SelectedTracks);
				MainViewModel.Message = (SelectedTracks.Count > 1) ? "Tracks added!" : "Track added!";
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
			}
			TargetPlaylist = null;
			SelectedTracks = null;
			SelectedTab.Loading = false;
			if (SelectedItems == null) NavigateToTracks();
			SelectedItems = null;
		}

		#endregion CommandsConfirms

		#region OpeningMethods

		public override async Task CommandTrack_Open(object arg)
		{
			var arg1 = arg as MusConvTrack;
			var sCriteria = $"{arg1.Title} {arg1.Artist}";

			await Task.Run(async () =>
			{
				try
				{
					AppleSearchResult aMusConvSearchResult = null;
					aMusConvSearchResult = await AppleUser.SearchTrack(sCriteria);

					if (aMusConvSearchResult != null && aMusConvSearchResult.Results.Songs.Data.Any())
                        OpenUrlExtension.OpenUrl(aMusConvSearchResult.Results.Songs.Data.FirstOrDefault()?.Attributes.Url);
				}
				catch
				{
				}
			}).ConfigureAwait(false);
		}

		#endregion OpeningMethods

		#region InnerMethods

		private void DeleteTracksFromPlaylist(MusConvPlayList playlist, IEnumerable<MusConvTrack> tracks)
		{
			XDocument xdoc = XDocument.Load(playlist.Description);
			var xmlPlaylists = xdoc.Element("plist").Element("dict").Element("array").Elements("dict").ToList();

			var tracksId = tracks.Select(x => x.Id);
			foreach (XElement xmlPlaylist in xmlPlaylists)
			{
				if (xmlPlaylist.Elements("string").Select(x => x.Value).Contains(playlist.Id))
				{
					var tracksOfXmlPlaylit = xmlPlaylist.Element("array")?.Elements("dict").ToList();
					if (tracksOfXmlPlaylit != null)
					{
						foreach (var trackOfXmlPlaylit in tracksOfXmlPlaylit)
						{
							if (tracksId.Contains(trackOfXmlPlaylit.Element("integer").Value))
							{
								trackOfXmlPlaylit.Remove();
							}
						}
					}
				}
			}
			xdoc.Save(playlist.Description);
			ItunesManager.FixXml(playlist.Description);
		}

		private void DeleteItemsFromSourceFile(List<MusConvItemBase> playlists)
		{
			foreach (MusConvModelBase playlistsOfCurrentFile in playlists)
			{
				if (playlistsOfCurrentFile?.Description != null)
				{
					XDocument xdoc = XDocument.Load(playlistsOfCurrentFile.Description);
					var xmlPlaylists = xdoc.Element("plist").Element("dict").Element("array").Elements("dict").ToList();
					xmlPlaylists.FirstOrDefault(p => p.Elements("string").Select(x => x.Value).Contains(playlistsOfCurrentFile.Id)
													 && p.Elements("string").Select(x => x.Value).Contains(playlistsOfCurrentFile.Title)).Remove();
					if (xmlPlaylists.Count == 0)
					{
						_manager.DeletePath(playlistsOfCurrentFile.Description, Paths.ITunesPlaylists);
					}

					xdoc.Save(playlistsOfCurrentFile.Description);
					ItunesManager.FixXml(playlistsOfCurrentFile.Description);
					MainViewModel.Message = $"Deleting {playlistsOfCurrentFile.Title}";
				}
			}
			MainViewModel.Message = "Playlists deleted successfully";
		}

		private void AddItemsToSourceFile(List<MusConvItemBase> playlists)
		{
			foreach (MusConvPlayList playlistsOfCurentFile in playlists)
			{
				AddPlaylistToSourceFile(playlistsOfCurentFile);
			}
		}

		private void AddPlaylistToSourceFile(MusConvPlayList playlist)
		{
			var xdoc = XDocument.Load(playlist.Description);
			var (root, pl) = GetPlaylistFromPlist(xdoc, playlist.Id);
			root.Add(pl);
			pl.Element("string").Value += " - clone";

			xdoc.Save(playlist.Description);
			ItunesManager.FixXml(playlist.Description);
		}

		private void EmptyPlaylists(List<MusConvItemBase> playlists)
		{
			foreach (MusConvPlayList playlistsOfCurentFile in playlists)
			{
				XDocument xdoc = XDocument.Load(playlistsOfCurentFile.Description);
				var xmlPlaylists = xdoc.Element("plist").Element("dict").Element("array").Elements("dict").ToList();
				xmlPlaylists.FirstOrDefault(p => p.Elements("string").Select(x => x.Value).Contains(playlistsOfCurentFile.Id)
												 && p.Elements("string").Select(x => x.Value).Contains(playlistsOfCurentFile.Title)).Element("array").RemoveAll();
				xdoc.Save(playlistsOfCurentFile.Description);
				ItunesManager.FixXml(playlistsOfCurentFile.Description);
			}
		}

		private void EmptyPlaylist(MusConvPlayList playlist)
		{
			XDocument xdoc = XDocument.Load(playlist.Description);
			var xmlPlaylists = xdoc.Element("plist").Element("dict").Element("array").Elements("dict").ToList();
			xmlPlaylists.FirstOrDefault(p => p.Elements("string").Select(x => x.Value).Contains(playlist.Id)
											 && p.Elements("string").Select(x => x.Value).Contains(playlist.Title)).Element("array").RemoveAll();
			xdoc.Save(playlist.Description);
			ItunesManager.FixXml(playlist.Description);
		}

		private void AddTracksToPlaylist(MusConvPlayList playlist, List<MusConvTrack> tracks)
		{

			XDocument xdoc = XDocument.Load(playlist.Description);
			var xmlPlaylists = xdoc.Element("plist").Element("dict").Element("array").Elements("dict").ToList();
			var xmlPlaylist = xmlPlaylists.FirstOrDefault(p =>
				p.Elements("string").Select(x => x.Value).Contains(playlist.Id) &&
				p.Elements("string").Select(x => x.Value).Contains(playlist.Title));
			if (xmlPlaylist.Element("array") == null)
			{
				xmlPlaylist.Add(new XElement("array"));
			}
			foreach (var track in tracks)
			{
				xmlPlaylist.Element("array").Add(new XElement("dict", new XElement("key", "Track ID"),
					new XElement("integer",
						track.Id)));
			}
			xdoc.Save(playlist.Description);
			ItunesManager.FixXml(playlist.Description);
		}

		private static (XElement Root, XElement Playlist) GetPlaylistFromPlist(XDocument xdoc, string id)
		{
			var xmlPlaylistRoot = xdoc.Element("plist").Element("dict").Element("array");
			var xmlPlaylists = xmlPlaylistRoot.Elements("dict").ToList();
			var xmlPlaylist = xmlPlaylists.FirstOrDefault(p => p.Elements("string").Select(x => x.Value).Contains(id));
			return (xmlPlaylistRoot, xmlPlaylist);
		}

		#endregion InnerMethods
	}
}