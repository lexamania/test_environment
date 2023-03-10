using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry.Extensions;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Abstractions.Extensions;
using ReactiveUI;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public sealed class EmbyViewModel : SectionViewModelBase
	{
		#region Constructors

		public EmbyViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Emby";

			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Emby;
			CurrentVMType = VmType.Service;
			LogoKey = LogoStyleKey.EmbyLogo;
			SmallLogoKey = LogoStyleKey.EmbyLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.EmbySideLogo;
			Model = new EmbyModel();
			BaseUrl = "https://emby.media/";
			NavigateHelpCommand = ReactiveCommand.Create(() =>
				m.NavigateTo(NavigationKeysChild.EmbyHelpPage));

			#region Tabs

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
			};

			#endregion Tabs

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
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon,
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon,
				EmptyTransfersBase, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon,
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
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

				if (!Model.IsAuthorized)
				{
					if (IsDeveloperLicense && SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data)
						&& await IsServiceDataExecuted(data))
					{
						return false;
					}
					MainViewModel.NavigateTo(NavigationKeysChild.EmailPasswordLoginForm);
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

		public override async Task InitialAuthorization()
		{
			RegState = RegistrationState.Logged;
			SelectedTab.Loading = false;
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

		public async Task Web_NavigatingAsync(object s, object t, object p)
		{
			var authorizationError = "";

			OnLoginPageLeft();
			try
			{
				await (Model as EmbyModel).AuthorizeAsync(s.ToString(), t.ToString(), p.ToString()).ConfigureAwait(false);
			}
			catch (System.Net.WebException e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
				return;
			}
            catch (Exception ex)
            {
                authorizationError = ex.Message;
            }

            if (!Model.IsAuthorized)
			{
				await ShowError($"Authorization failed\nReason: {authorizationError}");
				return;
			}

			if (IsDeveloperLicense)
			{
				SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(s as string, t as string) });
			}
			UserEmail = s.ToString();

            await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

            await InitialAuthorization();
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));

			foreach (var item in Tabs)
			{
				item.MediaItems.Clear();
			}
			
			await  Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			await Web_NavigatingAsync(serviceData["UserName"], serviceData["Password"], string.Empty);

			return true;
		}

		#endregion AuthMethods

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			MainViewModel.NavigateTo(NavigationKeysChild.EmbyHelpPage);
			return Task.CompletedTask;
		}

		#endregion Commands

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthenticated())
			{
				if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) || !await IsServiceDataExecuted(data))
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
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

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);

			foreach (var resultKey in result.Keys)
			{
				var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
				var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);

				MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

				foreach (var trackList in result[resultKey].Where(t => t?.ResultItems != null && t.ResultItems.Count > 0)
					.Select(x => x.ResultItems.First()).AllIsNotNull().ToList().SplitList())
				{
					await Model.AddTracksToPlaylist(createdPlaylist, trackList);
					progressReport.Report(new ReportCount(trackList.Count, $"Adding tracks to playlist \"{resultKey}\", please wait",
						ReportType.Sending, IsSelfTransfer));
				}
			}

			await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(result)));
		}

		#endregion TransferMethods

		#region InnerMethods

		private string GetSerializedServiceData(string s, string t)
		{
			return Serializer.Serialize(new Dictionary<string, string>
								{
									{ "UserName", s},
									{ "Password", t},
								});
		}

		#endregion InnerMethods
	}
}