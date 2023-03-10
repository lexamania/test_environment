using Avalonia.Threading;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System.Collections.Generic;
using static MusConv.MessageBoxManager.MessageBox;
using System.Threading.Tasks;
using MusConv.MessageBoxManager.Enums;
using System;
using MusConv.Sentry;
using MusConv.ViewModels.Models;
using System.Threading;
using MusConv.MessageBoxManager.Texts;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.WebViewViewModels.AuthHandlers;
using MusConv.Abstractions;
using System.Linq;
using MusConv.ViewModels.EventArguments;
using MusConv.Lib.Netease;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.WebViewViewModels
{
    public class NetEaseViewModel : WebViewModelBase
	{
		#region Constructors

        public NetEaseViewModel(MainViewModelBase m) : base(m, new(() => new NetEaseAuthHandler()))
        {
            Title = "NetEase";
            SourceType = DataSource.NetEase;
            LogoKey = LogoStyleKey.NetEaseLogo;
            SideLogoKey = LeftSideBarLogoKey.NetEaseSideLogo;
            RegState = RegistrationState.Unlogged;
            CurrentVMType = VmType.Service;
            Model = new NetEaseModel();
            Url = "https://music.163.com/";
            ArtistDirectUrl = "https://music.163.com/#/artist?id=";
            SearchUrl = "https://music.163.com/#/search/m/?s=";

			#region Commands

            var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
            {
                new ViewOnNetEaseCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
            {
                new ViewOnNetEaseCommand(CommandTrack_Open),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
            };

            var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
            {
                new ViewOnNetEaseCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
            };

			#endregion Commands

			#region TransferTasks

            var transferPlaylists = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send,m)
			};

			#endregion TransferTasks

			#region Tabs

            var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				transferPlaylists, commandPlaylistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));
            var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
                new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

            Tabs.Add(playlistsTab);
            Tabs.Add(tracksTab);
        }

		#endregion Constructors

		#region AuthMethods

        public override async Task<bool> IsServiceSelectedAsync()
        {
            try
            {
                MainViewModel.NeedLogin = this;
                await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

                if (!Model.IsAuthorized)
                {
                    if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
                    {
                        return false;
                    }

                    await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
                    return false;
                }

                await InitialUpdateForCurrentTab().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                MusConvLogger.LogFiles(ex);
                return false;
            }
        }

        public override async Task Web_NavigatingAsync(object s, object t)
        {
            /*if (string.IsNullOrEmpty(s?.ToString()) ||
				string.IsNullOrEmpty(t?.ToString()))
			{
				await ShowMessage("Enter credentials", Icon.Warning);
				return;
			}*/
                       
            var user = s as Dictionary<string, object>;
            if (user != null && t != null)
            {
                await Model.AuthorizeAsync(user["id"].ToString(), t.ToString()).ConfigureAwait(false);
            }

            if (!Model.IsAuthorized)
            {
                await ShowMessage("Authorization failed", Icon.Error);
                return;
            }
            NavigateToContent();
            SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(user["id"].ToString(), t.ToString()) });
            UserEmail = user["userName"].ToString();

            OnLoginPageLeft();
            await InitialAuthorization().ConfigureAwait(false);
        }

        public override async Task InitialAuthorization()
        {
            RegState = RegistrationState.Logged;
            if (IsSending)
            {
                WaitAuthentication.Set();
                IsSending = false;
            }
            else
            {
                await InitialUpdateForCurrentTab(true).ConfigureAwait(false);
                OnAuthReceived(new AuthEventArgs(Model));
            }
        }

        public override async Task<bool> Log_Out(bool forceUpdate = false)
        {
            SaveLoadCreds.DeleteServiceData();
            RegState = RegistrationState.Unlogged;
            await Model.Logout();
            OnLogoutReceived(new AuthEventArgs(Model));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var t in Tabs)
                    t.MediaItems.Clear();
                NavigateToBrowserLoginPage();
            });

            return true;
        }

        public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
        {
            var authData = Serializer.Deserialize<NetEaseCredentials>(data[Title].FirstOrDefault());
            await Model.AuthorizeAsync(authData.Id, authData.Token).ConfigureAwait(false);

            await InitialAuthorization().ConfigureAwait(false);
            return Model.IsAuthorized;
        }

		#endregion AuthMethods

		#region Commands

        public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
        {
            ShowHelp(MessageBoxText.NetEaseHelp);

            return Task.CompletedTask;
        }

		#endregion Commands

		#region TransferMethods

        public override async Task Transfer_SaveInTo(params object[] items)
        {
            MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
            IsSending = true;

            try
            {
                if (!Model.IsAuthenticated())
                {
                    if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) ||
                        !await IsServiceDataExecuted(data))
                    {
                        await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
                    }
                    else
                    {
                        await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
                    }
                }
                else
                {
                    WaitAuthentication.Set();
                }
            }
            catch (Exception ex)
            {
                MusConvLogger.LogFiles(ex);
            }

            await Transfer_DoWork(items[0]).ConfigureAwait(false);
            IsSelfTransfer = false;
            IsSending = false;
        }

        public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
           IProgress<ReportCount> progressReport, CancellationToken token)
        {
            var indexor = 0;
            MainViewModel.ResultVM.SetPlaylistSearchItem(result);
            foreach (var resultKey in result.Keys)
            {
                var allTracks = result[resultKey];
                allTracks.Reverse();

				var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
				var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);
                MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

                foreach (var item in allTracks)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        await Model.AddTracksToPlaylist(createdPlaylist, item.ResultItems);
                    }
                    catch (Exception ex)
                    {
                        MusConvLogger.LogFiles(ex);
                    }
                    progressReport.Report(new ReportCount(index,
                       $"Adding \"{result[resultKey][indexor].OriginalSearchItem.Title}\" to playlist \"{resultKey}\"",
                       ReportType.Sending));
                    indexor++;
                }
                indexor = 0;
            }
            await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(result)));
        }

		#endregion TransferMethods

		#region InnerMethods

        private string GetSerializedServiceData(string id, string token)
        {
            return Serializer.Serialize(new NetEaseCredentials { Id = id, Token = token });
        }

		#endregion InnerMethods
	}
}