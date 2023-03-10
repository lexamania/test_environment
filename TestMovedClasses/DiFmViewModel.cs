using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.MessageBoxManager;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class DiFmViewModel : SectionViewModelBase
	{
		#region Constructors

		public DiFmViewModel(MainViewModelBase mainViewModel) : base(mainViewModel)
		{
			Title = "DI.FM";
			Model = new DiFmModel();
			SourceType = DataSource.DiFm;
			LogoKey = LogoStyleKey.DiFmLogo;
			RegState = RegistrationState.Unlogged;
			CurrentVMType = VmType.Service;
			SideLogoKey = LeftSideBarLogoKey.DiFmSideLogo;
			BaseUrl = "https://www.di.fm/";

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			var tracksTab = new TrackTabViewModelBase(mainViewModel, AppTabs.LikedTracks, LwTabIconKey.TrackIcon,
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));
			
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;

				if (Model.IsAuthorized)
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
					await InitialUpdateForCurrentTab().ConfigureAwait(false);
					return true;
				}

				if (SaveLoadCreds.IsCredsExists(out var data) && await IsServiceDataExecuted(data))
				{
					return true;
				}
				
				await Log_Out();
				return false;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			var username = serviceData["Username"];
			var password = serviceData["Password"];

			await Model.Initialize(username, password).ConfigureAwait(false);
			if (!Model.IsAuthorized)
			{
				return false;
			}
			
			await Authorize(username, password).ConfigureAwait(false);
			return true;
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			await Model.Initialize(s, t).ConfigureAwait(false);

			if (!Model.IsAuthorized)
			{
				await MessageBox.ShowMessage("Authorization failed", Icon.Error);
				return;
			}

			await Authorize(s.ToString(), t.ToString()).ConfigureAwait(false);
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				foreach (var tab in Tabs)
					tab.MediaItems.Clear();

				NavigateToEmailPasswordLoginForm();
			});

			return true;
		}

		private async Task Authorize(string username, string password)
		{
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			UserEmail = username;
			await InitialAuthorization().ConfigureAwait(false);
			SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(username, password) });
		}

		#endregion AuthMethods

		#region InnerMethods

		private static string GetSerializedServiceData(string username, string password)
		{
			var credentials = new Dictionary<string, string>
			{
				["Username"] = username,
				["Password"] = password
			};
			return Serializer.Serialize(credentials);
		}

		#endregion InnerMethods
	}
}