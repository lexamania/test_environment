using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Abstractions.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class QobuzViewModel : SectionViewModelBase
	{
		#region Constructors

		public QobuzViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Qobuz";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Qobuz;
			LogoKey = LogoStyleKey.QobuzLogo;
			SideLogoKey = LeftSideBarLogoKey.QobuzSideLogo;
			CurrentVMType = VmType.Service;
			Model = new QobuzModel();
			BaseUrl = "https://www.qobuz.com/";
			ArtistDirectUrl = "https://www.qobuz.com/artist/";
			AlbumDirectUrl = "https://www.qobuz.com/album/";
			SearchUrl = "https://www.qobuz.com/search?q=";

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnQobuzCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandAlsoLikeTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnQobuzCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnQobuzCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ViewOnQobuzCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			#endregion Commands

			#region TransferTasks

			var transferPlaylist = new List<TaskBase_TaskItem>
			{ 
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m)
			};
			var transferTracks = new List<TaskBase_TaskItem>
			{ 
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m)
			};
			var transferAlbum = new List<TaskBase_TaskItem>
			{ 
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
			var transferArtist = new List<TaskBase_TaskItem>
			{ 
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m) 
			};

			#endregion TransferTasks

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				transferPlaylist, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				transferAlbum, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				transferTracks, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				transferArtist, new List<Command_TaskItem>(),
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var youMightAlsoLikeTab = new TrackTabViewModelBase(m, AppTabs.YouMightAlsoLike, LwTabIconKey.YouMightAlsoLikeTrackIcon, 
				transferTracks, commandAlsoLikeTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_YouMightAlsoLike), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(albumsTab);
			Tabs.Add(tracksTab);
			Tabs.Add(artistsTab);
			Tabs.Add(youMightAlsoLikeTab);
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
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
					{
						return false;
					}
					await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
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

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			MainViewModel.GoBack();
			OnLoginPageLeft();
			await Model.AuthorizeAsync(s.ToString(), t.ToString()).ConfigureAwait(false);

			if (!Model.IsAuthorized)
			{
				await ShowMessage("Authorization failed", Icon.Error);
				return;
			}

			SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(s.ToString(), t.ToString()) });
			UserEmail = s.ToString();
			
			await InitialAuthorization();
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
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));

			foreach (var tab in Tabs)
			{
				tab.MediaItems.Clear();
			}
			
			NavigateToEmailPasswordLoginForm();

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());

			await Model.AuthorizeAsync(serviceData["Login"], serviceData["Password"]);

			if (Model.IsAuthorized)
			{
				await InitialAuthorization();
				return true;
			}

			return false;
		}

		#endregion AuthMethods

		#region TransferMethods

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);

			int i = 0;
			foreach (var resultKey in result.Keys)
			{
				var list = result[resultKey]
					.Select(y => y.ResultItems?.FirstOrDefault())
					.Where(t => t is not null);

				var lt = list.ToList().SplitList(2000).ToList();

				foreach (var l1 in lt.Where(t => t.Count != 0))
				{
					var createModel = lt.Count == 1
						? new MusConvPlaylistCreationRequestModel(resultKey)
						: MusConvPlaylistCreationRequestModel.GetForPart(resultKey, lt.IndexOf(l1) + 1);
					var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);

					if (createdPlaylist is null)
						continue;

					MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

					var title = l1.First().Title;

					token.ThrowIfCancellationRequested();
					await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(new ReportCount(1,
						$"Adding \"{title}\" to playlist \"{resultKey}\"", ReportType.Sending)));
					try
					{
						await Model.AddTracksToPlaylist(createdPlaylist, l1).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						MusConvLogger.LogFiles(ex);
					}

					await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(GetPlaylistsReportCount(index, result)));
				}

				i++;
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

		private string GetSerializedServiceData(string login, string password)
		{
			return Serializer.Serialize(new Dictionary<string, string>
								{
									{ "Login", login},
									{ "Password", password},
								});
		}

		#endregion InnerMethods
	}
}