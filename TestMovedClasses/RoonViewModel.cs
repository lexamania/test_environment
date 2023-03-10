using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System.Collections.Generic;
using static MusConv.MessageBoxManager.MessageBox;
using System.Threading.Tasks;
using MusConv.MessageBoxManager.Texts;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import.Base;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[FileServiceAttribute]
	public class RoonViewModel : FileManagerViewModelBase
	{
		#region Constructors

		public RoonViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Roon";
			SourceType = DataSource.Roon;
			LogoKey = LogoStyleKey.RoonLogo;
			SideLogoKey = LeftSideBarLogoKey.RoonSideLogo;
			CurrentVMType = VmType.FileVM;
			RegState = RegistrationState.Needless;
			Model = new RoonModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ImportFileCommandBase(Commands.RoonImport, ImportFileCommand, CommandTaskType.CommandBar),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ImportFileCommandBase(Commands.RoonImport, ImportFileCommand, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ImportFileCommandBase(Commands.RoonImport, ImportFileCommand, CommandTaskType.CommandBar),
				new ViewOnYouTubeCommand(CommandTrack_Open)
			};
			
			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ImportFileCommandBase(Commands.RoonImport, ImportFileCommand, CommandTaskType.CommandBar),
				new ViewOnYouTubeCommand(CommandTrack_Open)
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ImportFileCommandBase(Commands.RoonImport, ImportFileCommand, CommandTaskType.CommandBar),
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Load more", Initial_Update_Playlists), commandTracks); 
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Load more", Initial_Update_Tracks), EmptyCommandsBase);
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab, 
				new Initial_TaskItem("Load more", Initial_Update_Artists), commandTracks);
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Load more", Initial_Update_Album), commandTracks);

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
			ShowHelp(MessageBoxText.RoonHelp);
			return Task.CompletedTask;
		}

		#endregion Commands
	}
}