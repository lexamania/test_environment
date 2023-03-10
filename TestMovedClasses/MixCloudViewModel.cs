using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Lib.MixCloud.Exceptions;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class MixCloudViewModel : SectionViewModelBase
	{
		#region Constructors

		public MixCloudViewModel(MainViewModelBase mainViewModel) : base(mainViewModel)
		{
			Title = "MixCloud";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.MixCloud;
			LogoKey = LogoStyleKey.MixCloudLogo;
			SideLogoKey = LeftSideBarLogoKey.MixCloudSideLogo;
			CurrentVMType = VmType.Service;
			BaseUrl = "https://www.mixcloud.com/";

			Model = new MixCloudModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, mainViewModel)
			};

			#endregion TransferTasks

			#region Tabs
			
			var showsTab = new ShowTabViewModelBase(mainViewModel, AppTabs.Favorites, LwTabIconKey.YouMightAlsoLikeTrackIcon,
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_FavoriteShows), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out), "Find in Shows"
			);

			var uploadsTab = new ShowTabViewModelBase(mainViewModel, AppTabs.Uploads, LwTabIconKey.UploadsTrackIcon,
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Uploads), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out), "Find in Shows"
			);

			var playlistsTab = new PlaylistTabViewModelBase(mainViewModel, LwTabIconKey.PlaylistIcon,
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out)
			);

			#endregion Tabs

			Tabs.Add(showsTab);
			Tabs.Add(uploadsTab);
			Tabs.Add(playlistsTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			var login = s.ToString();
			var password = t.ToString();
			await Model.AuthorizeAsync(login, password);

			if (!Model.IsAuthorized)
			{
				await ShowMessage("Authorization failed", Icon.Error);
				return;
			}
			
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			await InitialAuthorization(login, password);
		}

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (Model.IsAuthenticated())
				{
					await InitialUpdateForCurrentTab();
					return true;
				}

				if (SaveLoadCreds.IsCredsExists(out var data) && await IsServiceDataExecuted(data))
					return true;

				await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}

			return false;
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			await Model.Logout();
			SaveLoadCreds.DeleteServiceData();
			RegState = RegistrationState.Unlogged;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				ClearAllMediaItems();
				NavigateToEmailPasswordLoginForm();
			});

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			var login = serviceData["Login"];
			var password = serviceData["Password"];
			await Model.AuthorizeAsync(login, password);

			if (Model.IsAuthorized)
			{
				await InitialAuthorization(login, password);
				return true;
			}

			await Log_Out();
			return false;
		}

		private Task InitialAuthorization(string login, string password)
		{
			UserEmail = login;
			RegState = RegistrationState.Logged;
			SaveLoadCreds.SaveData(new List<string>() { GetSerializedServiceData(login, password) });

			if (IsSending)
			{
				WaitAuthentication.Set();
				IsSending = false;
				return Task.CompletedTask;
			}

			return InitialUpdateForCurrentTab(true);
		}

		#endregion AuthMethods

		#region InitialUpdate

		private Task<bool> Initial_Update_Uploads(bool forceUpdate = false)
		{
			var mixcloudService = Model as MixCloudModel;
			return InitialUpdateBuilder(mixcloudService.GetShows, SelectedTab, forceUpdate);
		}

		private Task<bool> Initial_Update_FavoriteShows(bool forceUpdate = false)
		{
			return InitialUpdateBuilder(Model.GetFavoritePlaylists, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region TransferMethods

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);

			foreach (var key in result.Keys)
			{
				var listSearchResults = result[key]
					.Where(t => t?.ResultItems != null && t?.ResultItems?.Count != 0);

				var playlist = new MusConvPlayList();

				//can't create empty playlist, playlist creates with the first track
				bool isFirstTrack = true;

				foreach (var searchResult in listSearchResults)
				{
					var track = searchResult.ResultItems?.FirstOrDefault();

					token.ThrowIfCancellationRequested();
					await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(new ReportCount(1,
						$"Adding \"{searchResult.OriginalSearchItem.Title}\" to playlist \"{key}\"",
						ReportType.Sending)));

					if (isFirstTrack)
					{
						//Playlist name must be unique, add any number at the end
						var ends = "";
						do
						{
							try
							{
								playlist = await Model.CreatePlaylist(new MusConvPlaylistCreationRequestModel(
									key.Title + ends, string.Empty, new[] { track })).ConfigureAwait(false);
								break;
							}
							catch (PlaylistAlreadyExistsException)
							{
								ends = new Random().Next(0, 10000).ToString();
							}
							catch (Exception e)
							{
								MusConvLogger.LogFiles(e);
							}
						}
						while (true);

						if (string.IsNullOrEmpty(playlist.Id))
							continue;
						MainViewModel.ResultVM.MediaItemIds.Add(playlist.Id);

						isFirstTrack = false;
					}
					else
					{
						try
						{
							var tracks = new List<MusConvTrack>() { track };
							await Model.AddTracksToPlaylist(playlist, tracks).ConfigureAwait(false);
						}
						catch (Exception ex)
						{
							MusConvLogger.LogFiles(ex);
						}
					}

					await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(index, result)));
				}
			}
		}

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthorized)
			{
				if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) || !await IsServiceDataExecuted(data))
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
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

		#region InnerMethods

		private static string GetSerializedServiceData(string login, string password)
		{
			var credentials = new Dictionary<string, string>
			{
				["Login"] = login,
				["Password"] = password
			};
			return Serializer.Serialize(credentials);
		}

		#endregion InnerMethods
	}
}