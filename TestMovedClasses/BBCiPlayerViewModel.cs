using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Models.MusicService.Base;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System.Collections.Generic;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class BBCiPlayerViewModel : BBCViewModelBase
	{
		#region Constructors

        public BBCiPlayerViewModel(MainViewModelBase m) : base(m)
        {
			Title = "BBC iPlayer";
			RegState = RegistrationState.Unlogged;
			Model = new BBCiPlayerModel();
			SourceType = DataSource.BBCiPlayer;
			LogoKey = LogoStyleKey.BBCiPlayerLogo;
			SideLogoKey = LeftSideBarLogoKey.BBCiPlayerSideLogo;

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open)
			};

			#endregion Commands

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			Tabs.Add(playlistTab);
		}

		#endregion Constructors
	}
}