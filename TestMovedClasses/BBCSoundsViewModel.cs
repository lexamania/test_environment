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
using System.Threading.Tasks;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class BBCSoundsViewModel : BBCViewModelBase
	{
		#region Constructors

		public BBCSoundsViewModel(MainViewModelBase m) : base(m)
		{
			Title = "BBCSounds";
			RegState = RegistrationState.Unlogged;
			Model = new BBCSoundsModel();
			SourceType = DataSource.BBCSounds;
			LogoKey = LogoStyleKey.BBCSoundsLogo;
			SideLogoKey = LeftSideBarLogoKey.BBCSoundsSideLogo;

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
			};

			#endregion Commands

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var subscriptionsTab = new PlaylistTabViewModelBase(m, "Subscriptions", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Subscriptions), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			Tabs.Add(playlistTab);
			Tabs.Add(subscriptionsTab);
		}

		#endregion Constructors

		#region InitialUpdate

        private Task<bool> Initial_Update_Subscriptions(bool forceUpdate)
        {
            return InitialUpdateBuilder((Model as BBCSoundsModel).GetSubscriptions, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate
	}
}