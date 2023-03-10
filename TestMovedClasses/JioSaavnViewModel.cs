using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MusConv.Sentry.Extensions;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.WebViewViewModels.AuthHandlers;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.WebViewViewModels
{
    public class JioSaavnViewModel : WebViewModelBase
	{
		#region Constructors

        public JioSaavnViewModel(MainViewModelBase m) : base(m, new(() => new JioSaavnAuthHandler()))
        {
            Title = "JioSaavn";
            RegState = RegistrationState.Unlogged;
            SourceType = DataSource.JioSaavn;
            LogoKey = LogoStyleKey.JioSaavnLogo;
            SideLogoKey = LeftSideBarLogoKey.SaavnSideLogo;
            SmallLogoKey = LogoStyleKey.JioSaavnLogoSmall;
            CurrentVMType = VmType.Service;
            Model = new JioSaavnModel();
            BaseUrl = "https://www.jiosaavn.com/";
            Url = "https://www.jiosaavn.com/login?redirect=/";

			#region Commands

            var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

            var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
            {
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
                new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
                new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
                new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
                new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
                new EditCommand(Command_Edit, CommandTaskType.CommandBar),
            };

            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
            {
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
                new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
                new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
                new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
                new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
                new EditCommand(Command_Edit, CommandTaskType.CommandBar),
            };

            var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
            };

            var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
            };

			#endregion Commands

			#region TransferTasks

            var transferTracks = new List<TaskBase_TaskItem>
            { 
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send, m)
			};
            var transferAlbums = new List<TaskBase_TaskItem>
            { 
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
            var transferArtists = new List<TaskBase_TaskItem>
            { 
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};
            var transferPlaylists = new List<TaskBase_TaskItem>
            { 
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

            var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				transferPlaylists, commandPlaylistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				transferTracks, commandTracksTab,
                new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            var albumTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				transferAlbums, commandAlbumsTab,
                new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var artistTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				transferArtists, commandArtistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

            Tabs.Add(playlistTab);
            Tabs.Add(tracksTab);
            Tabs.Add(albumTab);
            Tabs.Add(artistTab);
        }

		#endregion Constructors

		#region AuthMethods

        public override async Task<bool> IsServiceSelectedAsync()
        {
            try
            {
                MainViewModel.NeedLogin = this;
                await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

                if (!Model.IsAuthenticated())
                {
                    if (IsDeveloperLicense && SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
                    {
                        return false;
                    }

                    await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
                    return false;
                }
                else
                {
                    await InitialUpdateForCurrentTab().ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MusConvLogger.LogFiles(ex);
                return false;
            }
        }

        public override async Task Web_NavigatingAsync(object s, object t)
        {
            OnLoginPageLeft();
            try
            {
                if (s == null)
                    return;

                await Model.AuthorizeAsync((s as JioSaavnEventArguments).Cookies,"").ConfigureAwait(false);
                if (!Model.IsAuthenticated())
                {
                    await ShowError($"{Texts.AuthorizationFailed}! Check if credentials is correct or visit https://jiosaavn.com/forgot-password to recover.");
                    return;
                }

                if (IsDeveloperLicense)
                {
                    SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(s.ToString()) });
                }

                NavigateToContent();
                await InitialAuthorization().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
                await ShowError(e.Message);
                MusConvLogger.LogFiles(sentryEvent);
            }
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
            await Model.Logout().ConfigureAwait(false);
            RegState = RegistrationState.Unlogged;
            OnLogoutReceived(new AuthEventArgs(Model));

            foreach (var tab in Tabs)
            {
                tab.MediaItems.Clear();
            }

            NavigateToBrowserLoginPage();
            return true;
        }

        public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
        {
            var serviceData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
            await Web_NavigatingAsync(serviceData["Cookie"], serviceData["Password"]);

            return true;
        }

		#endregion AuthMethods

		#region TransferMethods

        public override async Task Transfer_SaveInTo(params object[] items)
        {
            MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
            IsSending = true;

            if (!Model.IsAuthenticated())
            {
                if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) || !await IsServiceDataExecuted(data))
                {
                    await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
                }
            }
            else
            {
                WaitAuthentication.Set();
                IsSending = false;
            }

            await Transfer_DoWork(items[0]);
        }

        public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
            IProgress<ReportCount> progressReport, CancellationToken token)
        {
            MainViewModel.ResultVM.SetPlaylistSearchItem(result);

            foreach (var resultKey in result.Keys)
            {
                var list = result[resultKey]
                    .Where(x => x != null)
                    .Select(x => x.ResultItems.First())
                    .ToList();
                token.ThrowIfCancellationRequested();
                progressReport?.Report(new ReportCount(list.Count,
                    $"Adding tracks to playlist \"{resultKey}\"",
                    ReportType.Sending));
                try
                {
					var createModel = new MusConvPlaylistCreationRequestModel(resultKey, list);
                    var playlist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);

                    MainViewModel.ResultVM.MediaItemIds.Add(playlist.Id);
                }
                catch (Exception ex)
                {
                    MusConvLogger.LogFiles(ex);
                }

            }

            progressReport.Report(GetPlaylistsReportCount(result));
        }

		#endregion TransferMethods

		#region InnerMethods

        private string GetSerializedServiceData(string cookie)
        {
            return Serializer.Serialize(new Dictionary<string, string>
                                {
                                    { "Cookie", cookie}
                                });
        }

		#endregion InnerMethods
	}
}