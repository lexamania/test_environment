using Avalonia.Threading;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using static MusConv.MessageBoxManager.MessageBox;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Lib.Yandex.Exceptions;
using MusConv.ViewModels.ViewModels.WebViewViewModels.AuthHandlers;
using MusConv.ViewModels.Settings;
using MusConv.Lib.Yandex.API;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.WebViewViewModels
{
    public class YandexMusicViewModel : WebViewModelBase
	{
		#region Constructors

        public YandexMusicViewModel(MainViewModelBase m) : base(m, new(() => new YandexAuthHandler()))
        {
            Title = "Yandex Music";
            RegState = RegistrationState.Unlogged;
            SourceType = DataSource.YandexMusic;
            LogoKey = LogoStyleKey.YandexMusicLogo;
            SideLogoKey = LeftSideBarLogoKey.YandexMusicSideLogo;
            CurrentVMType = VmType.Service;
            BaseUrl = "https://music.yandex.ru/";
            Url = string.Format(Urls.Yandex, YUserAPI.CLIENT_ID);
            Model = new YandexModel();

			#region Commands

            var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar)
            };

            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
            {
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
                new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
                new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
                new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
                new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
                new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
                new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
                new SplitCommand(Command_Split, CommandTaskType.CommandBar),
                new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
                new EditCommand(Command_Edit, CommandTaskType.CommandBar),
                new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
                new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.DropDownMenu),
                new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.CommandBar),
            };

            var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
            {
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
                new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
                new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
                new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
                new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
                new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
                new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
                new SplitCommand(Command_Split, CommandTaskType.CommandBar),
                new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
                new EditCommand(Command_Edit, CommandTaskType.CommandBar),
                new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
                new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.DropDownMenu),
                new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.CommandBar),
            };

            var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

            var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

            var commandFavouritesTab = new List<Command_TaskItem> (TracksTabCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
            };

			#endregion Commands

			#region TransferTasks

            var transferTracks = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m)
			};
            var transferPlaylist = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};
            var transferAlbum = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
            var transferArtist = new List<TaskBase_TaskItem>
			{
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m) 
			};

			#endregion TransferTasks

			#region Tabs

            var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				transferPlaylist, commandPlaylistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var likedPlaylistsTab = new PlaylistTabViewModelBase(m, AppTabs.LikedPlaylists, LwTabIconKey.PlaylistIcon, 
				transferPlaylist, commandPlaylistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Liked_Playlists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				transferArtist, commandArtistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				transferAlbum, commandPlaylistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var favoriteTrackTab = new TrackTabViewModelBase(m, AppTabs.FavoriteTracks, LwTabIconKey.TrackIcon, 
				transferTracks, commandFavouritesTab,
                new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            var playlistOfTheDayTab = new TrackTabViewModelBase(m, "Playlist of the day", LwTabIconKey.TrackIcon, 
				transferTracks, commandTracksTab,
                new Initial_TaskItem("Reload", Initial_Update_Particular_Tracks), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

            Tabs.Add(playlistsTab);
            Tabs.Add(likedPlaylistsTab);
            Tabs.Add(artistsTab);
            Tabs.Add(albumsTab);
            Tabs.Add(favoriteTrackTab);
            Tabs.Add(playlistOfTheDayTab);
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
                var token = s.ToString();

                await Model.Initialize(token).ConfigureAwait(false);

                if (!Model.IsAuthorized)
                {
                    await ShowMessage("Authorization failed", Icon.Error);
                    return;
                }

                await LoadUserAccountInfo(new List<string>() { token }).ConfigureAwait(false);
                SaveLoadCreds.SaveData(new List<string>() { token });
                await InitialAuthorization();
            }
            catch (YandexApiException ex)
            {
                await ShowError(ex.ErrorDescription);
                MusConvLogger.LogFiles(ex);
            }
            catch (Exception e)
            {
                await ShowError(e.Message);
                MusConvLogger.LogFiles(e);
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
            await Model.Logout().ConfigureAwait(false);
            RegState = RegistrationState.Unlogged;
            OnLogoutReceived(new AuthEventArgs(Model));
            foreach (var item in Tabs)
            {
                item.MediaItems.Clear();
            }

            await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
            return true;
        }

        public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
        {
            var serviceData = data.FirstOrDefault(x => x.Key == Title).Value;
            var token = serviceData.First();

            if ((await Model.Initialize(token)).LoginNavigationState != LoginNavigationState.Done)
            {
                await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
                return false;
            }

            if (Accounts.Count == 0)
            {
                await LoadUserAccountInfo(serviceData).ConfigureAwait(false);
            }

            await InitialAuthorization();
            return true;
        }

        public override async Task LoadUserAccountInfo(List<string> data)
        {
            foreach (var accountData in data)
            {

                var newModel = new YandexModel();
                await newModel.Initialize(accountData);

                var accInfo = new AccountInfo()
                {
                    Creds = accountData,
                    Name = newModel.Email
                };

                Accounts.Add(newModel, accInfo);
            }
        }

		#endregion AuthMethods

		#region InitialUpdate

        public override async Task<bool> InitialUpdateBuilder<T>(Func<Task<T>> getMethod, TabViewModelBase selectedTab, bool forceUpdate = false, bool overrideException = false)
        {
            try
            {
                return await base.InitialUpdateBuilder(getMethod, SelectedTab, forceUpdate, overrideException);
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.ProtocolError)
            {
                if (ex.Response is not HttpWebResponse response) throw;

                MusConvLogger.LogFiles(ex);

                await ShowError($"We can't receive data of your account, because the Yandex server responds with {response.StatusCode}");

                return false;
            }
        }

        private Task<bool> Initial_Update_Liked_Playlists(bool forceUpdate = false) =>
            InitialUpdateBuilder((Model as YandexModel)!.GetLikedPlaylists, SelectedTab, forceUpdate);

		#endregion InitialUpdate

		#region TransferMethods

        public override async Task Transfer_SaveInTo(params object[] items)
        {
            MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);


            IsSending = true;

            if (!Model.IsAuthorized)
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
                var tracks = new List<MusConvTrack>();

                foreach (var track in result[resultKey].Where(t => t?.ResultItems != null && t?.ResultItems?.Count != 0))
                {
                    try
                    {
                        var selectedTrack = track.ResultItems?.FirstOrDefault();

                        if (selectedTrack is null)
                        {
                            continue;
                        }

                        token.ThrowIfCancellationRequested();
                        await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(new ReportCount(1,
                                    $"Adding \"{track.OriginalSearchItem.Title}\" to playlist \"{resultKey}\"",
                                    ReportType.Sending)));

                        tracks.Add(selectedTrack);
                    }
                    catch (Exception ex)
                    {
                        MusConvLogger.LogFiles(ex);
                    }
                }

                //The yandex  library works when you add all the songs at once,
                //otherwise you need to constantly transmit a new playlist identifier to which it was added
				var createModel = new MusConvPlaylistCreationRequestModel(resultKey, tracks);
				var createdPlaylist = await Model.CreatePlaylist(createModel);

                MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);
            }

            await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(result)));
        }

		#endregion TransferMethods
	}
}