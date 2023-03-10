using Avalonia.Threading;
using MusConv.Lib.Reddit;
using MusConv.Lib.Reddit.Utils;
using MusConv.Abstractions;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.WebViewViewModels.AuthHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.Settings;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.WebViewViewModels
{
    public class RedditViewModel : WebViewModelBase
	{
		#region Fields

        private RedditModel RedditModel => Model as RedditModel;

		#endregion Fields

		#region Constructors

        public RedditViewModel(MainViewModelBase m) : base(m, new(() => new RedditAuthHandler()))
        {
            Title = "Reddit";
            RegState = RegistrationState.Unlogged;
            Model = new RedditModel();
            IsSuitableForAutoSync = false;
            RedditModel.Client.OnTokenExpired += OnTokenHasExpired;
            RedditModel.OnStateChanged += ChangeStatusMessage;
            SourceType = DataSource.Reddit;
            LogoKey = LogoStyleKey.RedditLogo;
            SideLogoKey = LeftSideBarLogoKey.RedditSideLogo;
            CurrentVMType = VmType.Service;
            Url = String.Format(Urls.Reddit, RedditClient.ClientId, RedditClient.State);
            BaseUrl = "https://www.reddit.com/";

			#region Commands

            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

            var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

            var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

            var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
            {
                new ViewOnYouTubeMusicCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			#region Tabs

            var playlistsTab = new PlaylistTabViewModelBase(m, AppTabs.Playlists, LwTabIconKey.PlaylistIcon,
                EmptyTransfersBase, commandPlaylistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var albumsTab = new AlbumTabViewModelBase(m, AppTabs.Albums, LwTabIconKey.AlbumIcon,
                EmptyTransfersBase, commandAlbumsTab,
                new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var artistsTab = new ArtistTabViewModelBase(m, AppTabs.Artists, LwTabIconKey.ArtistIcon,
                EmptyTransfersBase, commandArtistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

            Tabs.Add(playlistsTab);
            Tabs.Add(albumsTab);
            Tabs.Add(artistsTab);
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
                    if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
                    {
                        return true;
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

        public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
        {
            var creds = Serializer.Deserialize<RedditUserInfo>(data[Title].FirstOrDefault());

            if (!await IsModelAuthenticated(RedditModel, creds))
            {
                if (SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.LogOutOnLogInError))
                {
                    await Log_Out();
                }
                else
                {
                    await ShowAuthorizationError();
                }
                return false;
            }

            await Initialize(data[Title]).ConfigureAwait(false);
            await InitialAuthorization().ConfigureAwait(false);
            
            return true;
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
            Initial_Setup();
        }

        public override async Task Web_NavigatingAsync(object s, object t)
        {
            await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
            OnLoginPageLeft();

            var creds = Serializer.Serialize(s);

            if (!await IsModelAuthenticated(RedditModel, s as RedditUserInfo))
            {
                await ShowError("Authorization failed");
                await Dispatcher.UIThread.InvokeAsync(NavigateToMain);
                return;
            }

            await Initialize(new List<string>() { creds }).ConfigureAwait(false);
            await InitialAuthorization().ConfigureAwait(false);
            
        }

        public override async Task<bool> Log_Out(bool forceUpdate = false)
        {
            LogOutRequired = true;
            SaveLoadCreds.DeleteServiceData();
            RegState = RegistrationState.Unlogged;
            await Model.Logout().ConfigureAwait(false);
            OnLogoutReceived(new AuthEventArgs(Model));

            foreach (var item in Tabs)
            {
                item.MediaItems.Clear();
            }
            await Dispatcher.UIThread.InvokeAsync(NavigateToMain);
            return true;
        }

        private async Task Initialize(List<string> creds)
        {
            if (Accounts.Count == 0)
            {
                await LoadUserAccounts(creds);
                SaveLoadCreds.SaveData(Accounts.Values.Select(x => x.Creds).ToList());
            }
        }

        public async Task LoadUserAccounts(List<string> data)
        {
            Accounts.Clear();

            foreach (var accountData in data)
            {
                var credentials = Serializer.Deserialize<RedditUserInfo>(accountData);

                var newModel = new RedditModel();
                newModel.Client.OnTokenExpired += OnTokenHasExpired;
                await newModel.Initialize(credentials);

                var accInfo = new AccountInfo()
                {
                    Creds = accountData
                };

                Accounts.Add(newModel, accInfo);
            }
        }

        private async Task<bool> IsModelAuthenticated(RedditModel model, object creds)
        {
            if (!model.IsAuthenticated())
            {
                if ((await model.Initialize(creds)).LoginNavigationState == LoginNavigationState.Done)
                {
                    return false;
                }
            }

            return true;
        }

		#endregion AuthMethods

		#region InnerMethods

        private async Task OnTokenHasExpired(RedditUserInfo oldData, RedditUserInfo newData)
        {
            var oldCreds = Serializer.Serialize(oldData);
            var newCreds = Serializer.Serialize(newData);

            // Removing old credentials
            var accountWithOldCreds = Accounts.FirstOrDefault(x => x.Value.Creds == oldCreds).Key;

            if (accountWithOldCreds != null)
                Accounts.Remove(accountWithOldCreds);

            SaveLoadCreds.DeleteSingleServiceData(oldCreds);

            // Adding new credentials
            await LoadUserAccounts(new List<string>() { newCreds });

            RedditModel.Client = new(newData);
            RedditModel.Client.OnTokenExpired += OnTokenHasExpired;

            SaveLoadCreds.SaveData(Accounts.Values.Select(x => x.Creds).ToList());
        }

        private void ChangeStatusMessage(string str)
        {
            SelectedTab.LoadingText = str;
        }

		#endregion InnerMethods
	}
}