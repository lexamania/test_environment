using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class FreeTextViewModel : SectionViewModelBase
	{
		#region Constructors

        public FreeTextViewModel(MainViewModelBase m) : base(m)
        {
            RegState = RegistrationState.Needless;

            Title = "Free text";
            SourceType = DataSource.FreeText;
            LogoKey = LogoStyleKey.FreeTextLogo;
            SideLogoKey = LeftSideBarLogoKey.FreeTextSideLogo;
            Model = new FreeTextModel();

			#region Commands

            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
            {
                new EditCommand(Command_Edit, CommandTaskType.CommandBar),
            };

            var commandTracks = new List<Command_TaskItem> (TracksCommandsBase);

			#endregion Commands

			#region TransferTasks

            IsTransferAvailable = false;
            var playlistTransfer = new List<TaskBase_TaskItem>
            {
                new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, null, null, m)
            };

			#endregion TransferTasks

            var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon,
                playlistTransfer, commandPlaylistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks);

            Tabs.Add(playlistsTab);
        }

		#endregion Constructors

		#region AuthMethods

        public override async Task Web_NavigatingAsync(object s, object t)
        {
            SelectedTab.Loading = true;
            
            try
            {
                var parsedPlaylist = (Model as FreeTextModel).GetPlaylist(s.ToString());
                if(parsedPlaylist.AllTracks.Count > 0)
                {
                    NavigateToContent();
                    SelectedTab.MediaItems.Clear();
                    SelectedTab.MediaItems.Add(parsedPlaylist);
                }
                else
                    await ShowError(Texts.EnterCorrectTracks);            
            }
            catch (Exception ex)
            {
                MusConvLogger.LogFiles(ex);
                await ShowError(Texts.EnterCorrectTracks);
                MainViewModel.NavigateTo(NavigationKeysChild.FreeTextPage);
            }
            SelectedTab.Loading = false;
        }

        public override void SelectService()
        {
            MainViewModel.NeedLogin = this;
            MainViewModel.NavigateTo(NavigationKeysChild.FreeTextPage);
        }

		#endregion AuthMethods

		#region InitialUpdate

        public virtual void Initial_Update_Playlists(bool forceUpdate = false)
        {
            SelectService();
        }

		#endregion InitialUpdate
	}
}