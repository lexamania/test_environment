using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class Foobar2000ViewModel : M3UViewModelBase
	{
		#region Constructors

		public Foobar2000ViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Foobar2000";
			Extension = "m3u8";
			SourceType = DataSource.Foobar2000;
			LogoKey = LogoStyleKey.Foobar2000Logo;
			SideLogoKey = LeftSideBarLogoKey.Foobar2000SideLogo;
			RegState = RegistrationState.Needless;
			Model = new Foobar2000Model();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new ImportM3UFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, null, null, m)
			};

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks);

			Tabs.Add(playlistsTab);
		}

		#endregion Constructors
	}
}