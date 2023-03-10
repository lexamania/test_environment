using MusConv.MessageBoxManager.Texts;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.Models.MusicService;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[FileServiceAttribute]
	public class MixxxViewModel : SectionViewModelBase
	{
		#region Constructors

		public MixxxViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Mixxx";
			RegState = RegistrationState.Needless;
			SourceType = DataSource.Mixxx;
			CurrentVMType = VmType.FileVM;
			LogoKey = LogoStyleKey.MixxxLogo;
			SideLogoKey = LeftSideBarLogoKey.MixxxSideLogo;
			IsHelpVisible = true;
			Model = new M3UModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

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

		#region CommandsProperties

		public Task NavigateHelpCommand(object arg1, CommandTaskType commandTaskType)
		{
			MainViewModel.NavigateTo(NavigationKeysChild.MixxxHelpPage);
			return Task.CompletedTask;
		}

		#endregion CommandsProperties

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			MainViewModel.NavigateTo(NavigationKeysChild.Content);
			SelectedTab.LoadingText = MusConvConfig.PlayListLoading;

			return await InitialUpdateForCurrentTab().ConfigureAwait(false);
		}

		#endregion AuthMethods

		#region TransferMethods

		public override Task Transfer_SaveInTo(object[] items)
		{
			return ShowTransferToHelpOnDemandAsync(MessageBoxText.MixxxHelp);
		}

		#endregion TransferMethods
	}
}