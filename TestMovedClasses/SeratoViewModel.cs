using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Lib.Serato.Model;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class SeratoViewModel : SectionViewModelBase
	{
		#region Constructors

		public SeratoViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Serato DJ";
			RegState = RegistrationState.Unlogged;
			Model = new SeratoModel();
			SourceType = DataSource.Serato;
			LogoKey = LogoStyleKey.SeratoLogo;
			SideLogoKey = LeftSideBarLogoKey.SeratoSideLogo;
			Url = Urls.Serato;

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
			};

			var commandSubscriptionTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
			};

			#endregion Commands

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			Tabs.Add(playlistsTab);
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
			var credentials = Serializer.Deserialize<SeratoCreds>(serviceData.FirstOrDefault());

			if ((await Model.Initialize(credentials)).LoginNavigationState != LoginNavigationState.Done)
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

			var creds = s as SeratoCreds;

			if ((await Model.Initialize(creds)).LoginNavigationState != LoginNavigationState.Done)
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
				var credentials = Serializer.Deserialize<SeratoCreds>(accountData);

				var newModel = new SeratoModel();
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

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				foreach (var item in Tabs)
					item.MediaItems.Clear();

				NavigateToMain();
			});

			return true;
		}

		#endregion AuthMethods
	}
}