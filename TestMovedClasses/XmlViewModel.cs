using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System.Collections.Generic;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class XmlViewModel : XmlViewModelBase
	{
		#region Constructors

		public XmlViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Xml";
			Model = new XmlModel();
			SourceType = DataSource.Xml;
			LogoKey = LogoStyleKey.XmlLogo;
			SideLogoKey = LeftSideBarLogoKey.XmlSideLogo;
			IsSuitableForAutoSync = false;

			(Model as XmlModel).Manager.Parser.OnParsedItemChanged += (name) => SelectedTab.LoadingText = $"Loading {name}";

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ImportXmlFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ImportXmlFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ImportXmlFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open)
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
				new Initial_TaskItem("Load xml", Initial_Update_Playlists), commandTracks);
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon,
				transfer, commandAlbumsTab,
				new Initial_TaskItem("Load xml", Initial_Update_Album), commandTracks);
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon,
				transfer, commandArtistsTab,
				new Initial_TaskItem("Load xml", Initial_Update_Artists), commandTracks);

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
		}

		#endregion Constructors
	}
}