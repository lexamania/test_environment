using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Lib.Dailymotion;
using MusConv.Lib.ExternalApiClient.Models;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class DailymotionViewModel : SectionViewModelBase
	{
		#region Fields

		private DailymotionModel CurrentModel { get; set; }

		#endregion Fields

		#region Constructors

		public DailymotionViewModel(MainViewModelBase main) : base(main)
		{
			Title = "Dailymotion";
			SourceType = DataSource.Dailymotion;
			LogoKey = LogoStyleKey.DailymotionLogo;
			SideLogoKey = LeftSideBarLogoKey.DailymotionSideLogo;
			CurrentVMType = VmType.Service;
			RegState = RegistrationState.Unlogged;
			BaseUrl = "https://www.dailymotion.com/";
			CurrentModel = new DailymotionModel(OnTokenRefreshed);
			Model = CurrentModel;
			Url = Model.Url;

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			var playlistsTab = new PlaylistTabViewModelBase(main, LwTabIconKey.PlaylistIcon,
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			Tabs.Add(playlistsTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task SelectServiceAsync()
		{
			MainViewModel.NeedLogin = this;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			if (!Model.IsAuthorized)
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			}
			else
			{
				await InitialUpdateForCurrentTab().ConfigureAwait(false);
			}
		}

		public override async Task<bool> IsServiceSelectedAsync()
		{			
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (!Model.IsAuthenticated())
				{
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) &&
						await IsServiceDataExecuted(data))
					{						
						return true;
					}
					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
					return false;
				}
				else
				{
					await InitialUpdateForCurrentTab();
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
			try
			{				
				await Model.AuthorizeAsync(s as string, null).ConfigureAwait(false);

				if (!Model.IsAuthorized)
				{
					await ShowMessage("Authorization failed", Icon.Error);
					return;
				}
				SaveLoadCreds.SaveData(new List<string> { Serializer.Serialize(CurrentModel.GetAuthData()) });

				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				OnLoginPageLeft();

				await InitialAuthorization().ConfigureAwait(false);
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				Debug.WriteLine(e);
			}
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var authData = Serializer.Deserialize<DailymotionCredentials>(data[Title].FirstOrDefault());
			CurrentModel.SetTokens(authData.AccessToken, authData.RefreshToken);

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
        }

        public override bool IsAuthenticated()
		{
			return Model.IsAuthenticated();
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			SaveLoadCreds.DeleteServiceData();
			OnLogoutReceived(new AuthEventArgs(Model));

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				foreach (var tab in Tabs)
				{
					tab.MediaItems.Clear();
				}
				NavigateToMain();
			});

			return true;
		}

		#endregion AuthMethods

		#region InnerMethods

		private void OnTokenRefreshed(object sender, TokenRefreshEventArgs e)
		{
			var token = e.User.Token;
			SaveLoadCreds.SaveData(new List<string> 
			{ 
				Serializer.Serialize(new DailymotionCredentials { AccessToken = token.AccessToken, RefreshToken = token.RefreshToken }) 
			});
		}

		#endregion InnerMethods
	}
}