using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Lib.TuneFind.Models;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.Shared.SharedAbstractions.Enums;
using Client = MusConv.Lib.TuneFind.TuneFindClient;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class TuneFindViewModel : SectionViewModelBase
	{
		#region Fields

	    private readonly TuneFindModel _tuneFindModel;

		#endregion Fields

		#region Constructors

        public TuneFindViewModel(MainViewModelBase m) :  base(m)
		{
			Title = "TuneFind";
			RegState = RegistrationState.Unlogged;
			_tuneFindModel = new TuneFindModel();
			Model = _tuneFindModel;
			SourceType = DataSource.TuneFind;
			LogoKey = LogoStyleKey.TuneFindLogo;
			SideLogoKey = LeftSideBarLogoKey.TuneFindSideLogo;
			Url = Urls.TuneFind;

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			#endregion Commands

			#region TransferTasks

			var tracksTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send,  m, true)
			};

			#endregion TransferTasks

			#region Tabs

			var moviesTab = new PlaylistTabViewModelBase(m, "Movies", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			
			var tvShowsTab = new PlaylistTabViewModelBase(m, "TV Shows", LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Shows), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(m, AppTabs.LikedTracks, LwTabIconKey.TrackIcon, 
				tracksTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(moviesTab);
			Tabs.Add(tvShowsTab);
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
			var credentials = Serializer.Deserialize<TuneFindCreds>(serviceData.FirstOrDefault());

			await Model.Initialize(credentials);
			if (!Model.IsAuthorized)
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

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			OnLoginPageLeft();

			var creds = s as TuneFindCreds;

			await Model.Initialize(creds);
			if (!Model.IsAuthorized)
            {
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				return;
			}

			var json = Serializer.Serialize(creds);
			await LoadUserAccountInfo(new List<string>() { json }).ConfigureAwait(false);

			await InitialAuthorization();
		}

		public override async Task LoadUserAccountInfo(List<string> data)
		{
			foreach (var accountData in data)
			{
				var credentials = Serializer.Deserialize<TuneFindCreds>(accountData);

				var newModel = new TuneFindModel();
				await newModel.Initialize(credentials);

				var accInfo = new AccountInfo()
				{
					Creds = Serializer.Serialize(credentials),
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

			foreach (var item in Tabs)
				item.MediaItems.Clear();

			NavigateToMain();

			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

        private Task<bool> Initial_Update_Shows(bool forceUpdate)
        {
	        return InitialUpdateBuilder(_tuneFindModel.GetShows, SelectedTab, forceUpdate);
        }

		#endregion InitialUpdate

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