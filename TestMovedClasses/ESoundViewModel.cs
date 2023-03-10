using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using System;
using MusConv.Sentry;
using System.Threading;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.Abstractions.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class ESoundViewModel : SectionViewModelBase
	{
		#region Constructors

		public ESoundViewModel(MainViewModelBase m) : base(m)
		{
			Title = "eSound";
			RegState = RegistrationState.Unlogged;
			CurrentVMType = VmType.Service;
			SourceType = DataSource.ESound;
			LogoKey = LogoStyleKey.ESoundLogo;
			SideLogoKey = LeftSideBarLogoKey.ESoundSideLogo;
			Model = new ESoundModel();
			Url = Urls.ESound;

			#region Tabs

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase);

			#endregion Tabs

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_SearchWithAlbums, Transfer_Send, m, true)
			};

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon,
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			Tabs.Add(playlistTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			if (!Model.IsAuthenticated())
			{
				if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data)
					&& await IsServiceDataExecuted(data))
				{
					await InitialAuthorization().ConfigureAwait(false);
					return true;
				}

				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			}
			else
			{
				await InitialUpdateForCurrentTab().ConfigureAwait(false);
			}

			return true;
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			OnLoginPageLeft();

			var newModel = Model.IsAuthenticated() ? new ESoundModel() : Model;

			if (!await IsModelAuthenticated(newModel as ESoundModel, s, t))
			{
				await ShowError("Authorization failed");
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				return;
			}

			var esoundModel = newModel as ESoundModel;
			Accounts[newModel] = new AccountInfo()
			{
				Creds = Serializer.Serialize(new Dictionary<string, string>
				{
					{ "AccessToken", esoundModel.AccessToken},
					{ "RefreshToken", esoundModel.RefreshToken},
					{ "UserId", esoundModel.Email }
				}),
				Name = esoundModel.Email
			};

			Model = esoundModel;

			await InitialAuthorization().ConfigureAwait(false);
		}

		public override async Task InitialAuthorization()
		{
			RegState = RegistrationState.Logged;

			await InitialUpdateForCurrentTab(true).ConfigureAwait(false);
			OnAuthReceived(new AuthEventArgs(Model));

			SaveLoadCreds.SaveData(Accounts.Values.Select(x => x.Creds).ToList());
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteSingleServiceData(Accounts.First().Value.Creds);
			Accounts.Clear();

			await Model.Logout();
			Model = new ESoundModel();
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));

			foreach (var item in Tabs)
			{
				item.MediaItems.Clear();
			}

			await Dispatcher.UIThread.InvokeAsync(NavigateToMain);
			return true;
		}

		public override async Task<bool> IsAccountAuthenticated(MusicServiceBase model, AccountInfo accountInfo)
		{
			var serviceData = Serializer.Deserialize<Dictionary<string, string>>(accountInfo.Creds);

			if (await IsModelVerified(model as ESoundModel, serviceData["AccessToken"], serviceData["RefreshToken"]))
			{
				return true;
			}

			SaveLoadCreds.DeleteSingleServiceData(accountInfo.Creds);
			Accounts.Clear();
			return false;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			if (Accounts.Count > 0 && await IsAccountAuthenticated(Model, Accounts.First().Value))
			{
				await InitialAuthorization().ConfigureAwait(false);
				return true;
			}

			await LoadUserAccountInfo(data.FirstOrDefault(x => x.Key == Title).Value).ConfigureAwait(false);

			return false;
		}

		public override Task LoadUserAccountInfo(List<string> data)
		{
			foreach (var accountData in data)
			{
				var serviceData = Serializer.Deserialize<Dictionary<string, string>>(accountData);
				if (serviceData.ContainsKey("UserId"))
				{
					Accounts.Add(new ESoundModel(), new AccountInfo()
					{
						Creds = accountData,
						Name = serviceData["UserId"]
					});
				}
			}

            return Task.CompletedTask;
        }

		private async Task<bool> IsModelAuthenticated(ESoundModel model, object authCode, object authCodeVerifier)
		{
			return model.IsAuthenticated() ||
			       (await model.Initialize(authCode, authCodeVerifier)).LoginNavigationState == LoginNavigationState.Done;
		}

		#endregion AuthMethods

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			try
			{
				await IsServiceSelectedAsync().ConfigureAwait(false);

				if (!Model.IsAuthenticated())
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				}
				else
				{
					WaitAuthentication.Set();
					IsSending = false;
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}

			await Transfer_DoWork(items[0]);
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result,
			int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			try
			{
				MainViewModel.ResultVM.SetPlaylistSearchItem(result);

				foreach (var resultKey in result.Keys)
				{
					var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
					var createdPlaylist = await ModelTransferTo.CreatePlaylist(createModel).ConfigureAwait(false);

					MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);
					foreach (var it in result[resultKey].Where(t => t?.ResultItems != null && t.ResultItems.Count > 0)
								 .Select(x => x.ResultItems.FirstOrDefault()).ToList()
								 .SplitList())
					{
						await ModelTransferTo.AddTracksToPlaylist(createdPlaylist, it);
						progressReport.Report(new ReportCount(it.Count,
							$"Adding tracks to playlist \"{resultKey}\", please wait",
							ReportType.Sending, IsSelfTransfer));
					}

					await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(result)));
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}
		}

		#endregion TransferMethods

		#region InnerMethods

		private async Task<bool> IsModelVerified(ESoundModel model, string accessToken, string refreshToken)
		{
			return model.IsAuthenticated() || await model.IsTokenAuthorizedAsync(accessToken, refreshToken);
		}

		#endregion InnerMethods
	}
}