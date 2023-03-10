using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MusConv.Abstractions;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using Newtonsoft.Json;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.WebViewViewModels.AuthHandlers;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.WebViewViewModels
{
	public class GaanaViewModel : WebViewModelBase
	{
		#region Constructors

		public GaanaViewModel(MainViewModelBase m) : base(m, new(() => new GaanaAuthHandler()))
		{
			Title = "Gaana";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Gaana;
			LogoKey = LogoStyleKey.GaanaLogo;
			SmallLogoKey = LogoStyleKey.GaanaLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.GaanaSideLogo;
			CurrentVMType = VmType.Service;
			Model = new GaanaModel();
			BaseUrl = Urls.Gaana;
			Url = Urls.Gaana;

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu) ,
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new EditCommand(Command_Edit, CommandTaskType.CommandBar),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar)
			};

			#endregion Commands

			#region TransferTasks

			var transferPlaylist = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				transferPlaylist, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var favoriteTrackTab = new TrackTabViewModelBase(m, AppTabs.FavoriteTracks, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(favoriteTrackTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			OnLoginPageLeft();
			if (s is not GaanaEventArguments eventArgs) return;

			var cookiesList = eventArgs.Cookies.ToList();
			//SelectedTab.LoadingText = MusConvConfig.PlayListLoading;
			await Model.AuthorizeAsync(cookiesList, t).ConfigureAwait(false);

			if (!Model.IsAuthorized)
			{
				await ShowWarning("Authorization failed");
				await Dispatcher.UIThread.InvokeAsync(NavigateToMain);
				return;
			}
			SaveLoadCreds.SaveData(new List<string> { JsonConvert.SerializeObject(cookiesList) });

			await InitialAuthorization().ConfigureAwait(false);
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

				await InitialUpdateForCurrentTab().ConfigureAwait(false);
				return true;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			RegState = RegistrationState.Unlogged;
			Model.IsAuthorized = false;
			OnLogoutReceived(new AuthEventArgs(Model));

			foreach (var tab in Tabs)
			{
				tab.MediaItems.Clear();
			}

			await Dispatcher.UIThread.InvokeAsync(() => NavigateToMain());
			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<List<Cookie>>(data[Title].FirstOrDefault());
			await Model.AuthorizeAsync(serviceData, null).ConfigureAwait(false);

			return Model.IsAuthorized;
		}

		#endregion AuthMethods

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

			await Transfer_DoWork(items[0]).ConfigureAwait(false);
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);

			if (result != null)
			{
				foreach (var resultKey in result.Keys)
				{
					var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
					var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);

					if (string.IsNullOrEmpty(createdPlaylist.Id))
					{
						continue;
					}
					MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

					foreach (var track in result[resultKey].Where(t => t?.ResultItems != null && t?.ResultItems?.Count != 0))
					{
						try
						{
							var selectedTrack = track.ResultItems?.FirstOrDefault();
							if (selectedTrack == null) continue;

							token.ThrowIfCancellationRequested();
							progressReport.Report(new ReportCount(1,
										$"Adding \"{track.OriginalSearchItem.Title}\" to playlist \"{resultKey}\"",
										ReportType.Sending));

							var tracks = new List<MusConvTrack> { selectedTrack };
							await Model.AddTracksToPlaylist(createdPlaylist, tracks).ConfigureAwait(false);
						}
						catch (Exception ex)
						{
							MusConvLogger.LogFiles(ex);
						}
					}
				}
			}

			progressReport.Report(GetPlaylistsReportCount(result));
		}

		#endregion TransferMethods
	}
}