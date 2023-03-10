using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Abstractions.Extensions;
using MusConv.MessageBoxManager.Texts;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Sentry.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Settings;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.WebViewViewModels.AuthHandlers;
using PandoraSharp;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.WebViewViewModels
{
	public class PandoraViewModel : WebViewModelBase
	{
		#region Fields

		public bool AuthorizePending { get; set; } = true;
		private static bool GetOnlyLikedTracksFromStations
		{
			get => SettingsViewModel.IsSettingOptionRequired(SettingsOptionType
				.DisplayOnlyLikedTracksFromStationsPandora);
		}

		#endregion Fields

		#region Constructors

		public PandoraViewModel(MainViewModelBase m) : base(m, new(() => new PandoraAuthHandler()))
		{
			Title = "Pandora";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Pandora;
			LogoKey = LogoStyleKey.PandoraLogo;
			SideLogoKey = LeftSideBarLogoKey.PandoraSideLogo;
			Model = new PandoraModel();
			Url = Urls.Pandora;
			BaseUrl = Urls.Pandora;
			IsHelpVisible = true;
			AlbumDirectUrl = "https://www.pandora.com/album/";

			NavigateHelpCommand = ReactiveCommand.Create(() =>
				m.NavigateTo(NavigationKeysChild.PandoraHelp));

			#region Commands

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandStationTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new LoadMoreSongsCommand(Command_LoadStations),
				new LoadMoreSongsCommand(Command_MultipLoadStations, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewOnPandoraCommand(CommandArtist_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};
			var albumTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, AppTabs.Playlists, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var stationsTab = new PlaylistTabViewModelBase(m, AppTabs.Stations, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandStationTab,
				new Initial_TaskItem("Reload", Initial_Update_Stations), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var likedTracksTab = new TrackTabViewModelBase(m, AppTabs.LikedTracks, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(m, AppTabs.Albums, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistTab = new ArtistTabViewModelBase(m, AppTabs.FollowedArtists, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			// TODO: loads only first 50 songs for "Songs" tab, to fix
			var songsTab = new TrackTabViewModelBase(m, AppTabs.Songs, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Songs), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(stationsTab);
			Tabs.Add(likedTracksTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistTab);
			Tabs.Add(songsTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (Model.IsAuthenticated())
				{
					await InitialUpdateForCurrentTab().ConfigureAwait(false);
					return true;
				}

				if (SaveLoadCreds.IsCredsExists(out var data) && await IsServiceDataExecuted(data))
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
					return true;
				}

				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}

			return false;
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			var eventArgs = s as PandoraEventArgs;
			var csrfToken = eventArgs.Creds.CsrfToken;
			var authToken = eventArgs.Creds.AuthToken;

			if (!await Initialize(csrfToken, authToken))
				return;

			await InitialAuthorization().ConfigureAwait(false);
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			await Model.Logout().ConfigureAwait(false);
			LogOutRequired = true;
			Model = new PandoraModel();
			AuthorizePending = true;
			SaveLoadCreds.DeleteServiceData();
			RegState = RegistrationState.Unlogged;
			(Model as PandoraModel).Client = new Pandora();

			foreach (var item in Tabs)
			{
				item.MediaItems.Clear();
			}

			await Dispatcher.UIThread.InvokeAsync(NavigateToMain);
			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var pandoraData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			var cookieData = Serializer.Deserialize<Cookie>(pandoraData["Cookie"]);
			var cookie = new Cookie
			{
				Name = cookieData.Name,
				Value = cookieData.Value,
				Domain = cookieData.Domain,
				Path = cookieData.Path,
			};

			if (await Initialize(cookie, pandoraData["Token"]))
			{
				await InitialAuthorization().ConfigureAwait(false);
				return true;
			}

			if (SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.LogOutOnLogInError))
			{
				(Model as PandoraModel).Client = new Pandora();
				AuthorizePending = true;
			}
			else
			{
				await ShowAuthorizationError();
			}

			return false;
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
			}
		}

		private async Task<bool> Initialize(object cookie, object token)
		{
			if (cookie is null || !AuthorizePending)
				return false;

			try
			{
				await Model.Initialize((Cookie)cookie, token.ToString());

				SaveLoadCreds.SaveData(new List<string>
				{
					GetSerializedServiceData(cookie as Cookie, token.ToString())
				});

				AuthorizePending = false;
				return true;
			}
			catch (Exception ex)
			{
				var sentryEvent = SentryEventBuilder.Build(ex).WithVisibleTag(true);
				MusConvLogger.LogFiles(sentryEvent);
				await ShowError(ex.Message);
				return false;
			}
		}

		#endregion AuthMethods

		#region InitialUpdate

		public Task<bool> Initial_Update_Songs(bool forceUpdate = false)
		{
			return InitialUpdateBuilder(Model.GetTracks, SelectedTab, forceUpdate);
		}

		public Task<bool> Initial_Update_Stations(bool forceUpdate = false)
		{
			return InitialUpdateBuilder(() => (Model as PandoraModel).GetStations(GetOnlyLikedTracksFromStations), SelectedTab,
				forceUpdate);
		}

		#endregion InitialUpdate

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			return ShowHelp(MessageBoxText.PandoraHelp);
		}

		private async Task Command_LoadStations(object parameters)
		{
			try
			{
				var arg = parameters as List<MusConvModelBase>;
				var station = arg.First() as MusConvWithTracksBase;

				SelectedTab.Loading = true;

				station.AllTracks =
					await (Model as PandoraModel).GetAllTracksByStationId(station.Id, GetOnlyLikedTracksFromStations);

				SelectedTab.Loading = false;
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
			}
		}

		private async Task Command_MultipLoadStations(object arg, CommandTaskType commandTaskType)
		{
			try
			{
				SelectedTab.Loading = true;

				var stations = SelectedTab.MediaItems?.Where(p => p.IsSelected).Cast<MusConvWithTracksBase>().ToList();

				foreach (var station in stations)
				{
					station.AllTracks =
						await (Model as PandoraModel).GetAllTracksByStationId(station.Id,
							GetOnlyLikedTracksFromStations);
				}

				SelectedTab.Loading = false;

				if (!(stations?.Count > 0))
				{
					await ShowError("No stations selected. Please select at least one station.");
				}
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
			}
		}

		#endregion Commands

		#region OpeningMethods

		private Task CommandArtist_Open(object arg)
		{
			var arg1 = arg as MusConvArtist;
			string title = arg1.Title.Replace(' ', '-');
            OpenUrlExtension.OpenUrl($"https://www.pandora.com/artist/{title}/{arg1.Id}");
			return Task.CompletedTask;
		}

		#endregion OpeningMethods

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthenticated())
			{
				if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) ||
					!await IsServiceDataExecuted(data))
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

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result,
			int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			try
			{
				MainViewModel.ResultVM.SetPlaylistSearchItem(result);

				foreach (var resultKey in result.Keys)
				{
					token.ThrowIfCancellationRequested();

					progressReport.Report(new ReportCount(index, $"Adding tracks to playlist \"{resultKey}\"",
						ReportType.Sending));

					var tracks = result[resultKey].Where(t => t.ResultItems?.Count > 0)
												.Select(x => x.ResultItems.FirstOrDefault()).ToArray();

					try
					{
						var createModel = new MusConvPlaylistCreationRequestModel(resultKey, tracks);
						var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);
						MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);
					}
					catch (Exception ex)
					{
						MusConvLogger.LogFiles(ex);
					}
				}

				progressReport.Report(GetPlaylistsReportCount(index, result));
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}
		}

		#endregion TransferMethods

		#region InnerMethods

		private static string GetSerializedServiceData(Cookie cookie, string token)
		{
			return Serializer.Serialize(new Dictionary<string, string>
			{
				{ "Cookie", Serializer.Serialize(cookie) },
				{ "Token", token },
			});
		}

		#endregion InnerMethods
	}
}