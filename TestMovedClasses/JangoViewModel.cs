using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Lib.Jango.Models;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.WebViewViewModels.AuthHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusConv.ViewModels.ViewModels.WebViewViewModels
{
    public class JangoViewModel : WebViewModelBase
	{
		#region Constructors

        public JangoViewModel(MainViewModelBase m) : base(m, new(() => new JangoAuthHandler()))
        {
            Title = "Jango";
            RegState = RegistrationState.Unlogged;
            Model = new JangoModel();
            SourceType = DataSource.Jango;
            LogoKey = LogoStyleKey.JangoLogo;
            SideLogoKey = LeftSideBarLogoKey.JangoSideLogo;
            Url = Urls.Jango;

			#region Commands

            var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

            var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
            };

            var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
            };

			#endregion Commands

			#region TransferTasks

            var tracksTransfer = new List<TaskBase_TaskItem>
            {
                new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack,Transfer_SearchWithAlbums, TransferTrack_Send, m, true)
            };

			#endregion TransferTasks

			#region Tabs

            var tracksTab = new TrackTabViewModelBase(m, AppTabs.AmazonLikedTracks, LwTabIconKey.TrackIcon, 
				tracksTransfer, commandTracksTab,
                new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

            Tabs.Add(tracksTab);
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
                    if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data)
                        && await IsServiceDataExecuted(data))
                    {
                        return true;
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

        public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
        {
            var serviceData = data.FirstOrDefault(x => x.Key == Title).Value;
            var credentials = Serializer.Deserialize<JangoCreds>(serviceData.FirstOrDefault());

            if ((await Model.Initialize(credentials)).LoginNavigationState != LoginNavigationState.Done)
            {
                await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
                return false;
            }

            if (Accounts.Count == 0)
            {
                LoadUserAccountInfo(serviceData);
            }

            await InitialAuthorization();
            return true;
        }

        public override async Task Web_NavigatingAsync(object s, object t)
        {
            var creds = s as Dictionary<string, string>;

            var jangoCreds = new JangoCreds
            {
                CookieString = creds["Cookie"],
                UserId = creds["UserId"],
                CsrfToken = creds["Csrf"]
            };

            if ((await Model.Initialize(jangoCreds)).LoginNavigationState != LoginNavigationState.Done)
            {
                await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
                return;
            }

            var json = Serializer.Serialize(jangoCreds);
            LoadUserAccountInfo(new List<string>() { json });

            await InitialAuthorization();
        }

        public async void LoadUserAccountInfo(List<string> data)
        {
            foreach (var accountData in data)
            {
                var credentials = Serializer.Deserialize<JangoCreds>(accountData);

                var newModel = new JangoModel();
                await newModel.Initialize(credentials);

                var accInfo = new AccountInfo()
                {
                    Creds = accountData,
                    Name = newModel.Email
                };

                Accounts.Add(newModel, accInfo);
            }
        }

        public override async Task<bool> Log_Out(bool forceUpdate = false)
        {
            SaveLoadCreds.DeleteServiceData();
            Accounts.Remove(Model);
            await Model.Logout().ConfigureAwait(false);
            RegState = RegistrationState.Unlogged;
            LogOutRequired = true;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var item in Tabs)
                    item.MediaItems.Clear();

                NavigateToMain();
            });

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
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
                }
            }
            else
            {
                WaitAuthentication.Set();
                IsSending = false;
            }

            await Transfer_DoWork(items[0]);
        }

		#endregion TransferMethods
	}
}