using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Lib.HypeMachine;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
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
using System.Linq;
using System.Threading.Tasks;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class HypeMachineViewModel:SectionViewModelBase
	{
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

					await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
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
			await Model.AuthorizeAsync(s.ToString(), t.ToString()).ConfigureAwait(false);

			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			UserEmail = s.ToString();
			SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(s.ToString(), t.ToString()) });

			OnLoginPageLeft();
			await InitialAuthorization().ConfigureAwait(false);
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var authData = Serializer.Deserialize<HypeMachineCredentials>(data[Title].FirstOrDefault());
			await Model.AuthorizeAsync(authData.Login, authData.Password);
			UserEmail = authData.Login;
			await InitialAuthorization().ConfigureAwait(false);
			return Model.IsAuthorized;
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
			Model.IsAuthorized = false;
			OnLogoutReceived(new AuthEventArgs(Model));

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				ClearAllMediaItems();
				NavigateToEmailPasswordLoginForm();
			});

			return true;
		}

		#endregion AuthMethods

		#region InnerMethods

		public HypeMachineViewModel(MainViewModelBase main):base(main)
		{
			Title = "HypeMachine";
			SourceType = DataSource.HypeMachine;
			CurrentVMType = VmType.Service;
			LogoKey = LogoStyleKey.HypeMachineLogo;
			SideLogoKey = LeftSideBarLogoKey.HypeMachineSideLogo;
			RegState = RegistrationState.Unlogged;
			Model = new HypeMachineModel();
			BaseUrl = "https://hypem.com/";
			//don`t have public api for add tracks to playlists
			IsSuitableForAutoSync = false;

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands


			IsTransferAvailable = false;

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(main, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(main, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase, 
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Commands

			Tabs.Add(playlistsTab);
		}

		private string GetSerializedServiceData(string s, string t)
		{
			return Serializer.Serialize(new HypeMachineCredentials { Login = s, Password = t });
		}

		#endregion InnerMethods
	}
}