using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.Models;
using System.Threading;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.EventArguments;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class IBroadcastViewModel : SectionViewModelBase
	{
		#region Constructors

		public IBroadcastViewModel(MainViewModelBase m) : base(m)
		{
			Title = "iBroadcast";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.iBroadcast;
			LogoKey = LogoStyleKey.IBroadcastLogo;
			SideLogoKey = LeftSideBarLogoKey.IBroadcastSideLogo;
			CurrentVMType = VmType.Service;
			//Service cannot be a destination for auto sync because can't add tracks from request,
			//need to upload by yourself on the service site
			IsSuitableForAutoSync = false;
			Model = new IBroadcastModel();
			BaseUrl = "https://media.ibroadcast.com/";

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandAlbumsTabs = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandArtistsTabs = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{ 
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTabs,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTabs,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(albumsTab);
			Tabs.Add(tracksTab);
			Tabs.Add(artistsTab);
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
			await Model.AuthorizeAsync(s.ToString(), t.ToString()).ConfigureAwait(false);

			if (!Model.IsAuthorized)
			{
				await ShowMessage("Authorization failed", Icon.Error);
				return;
			}

			SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(s.ToString(), t.ToString()) });
			await Dispatcher.UIThread.InvokeAsync(() => NavigateToContent());
			OnLoginPageLeft();

			UserEmail = s.ToString();
			RegState = RegistrationState.Logged;
			if (IsSending)
			{
				WaitAuthentication.Set();
				IsSending = false;
			}
			else
			{
				await Initial_Update_Playlists().ConfigureAwait(false);
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
			await Web_NavigatingAsync(serviceData["Login"], serviceData["Password"]);

			return true;
		}

		#endregion AuthMethods

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
			token.ThrowIfCancellationRequested();
			foreach (var resultKey in result.Keys)
			{
				token.ThrowIfCancellationRequested();
				progressReport.Report(new ReportCount(index, $"Adding tracks to playlist \"{resultKey}\"",
					ReportType.Sending));
				var items = result[resultKey].Where(t => t.ResultItems?.Count > 0 && t.ResultItems.FirstOrDefault() is not null)
								.Select(x => x.ResultItems.FirstOrDefault());
				try
				{
					await Model.CreatePlaylist(new MusConvPlaylistCreationRequestModel(resultKey, items)).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					MusConvLogger.LogFiles(ex);
				}
				progressReport.Report(GetPlaylistsReportCount(index, result));
			}
		}

		public override async Task<MusConvTrackSearchResult> Transfer_Search(int index, MusConvTrack track, IProgress<ReportCount> arg3, CancellationToken token)
		{
			try
			{
				var search = $"\"{track.Title}\" by \"{track.Artist}\"";
				arg3.Report(new ReportCount(index, $"Searching: {search} ", ReportType.Searching));
				token.ThrowIfCancellationRequested();
				
				var tracks = await ModelTransferTo.SearchTrack(new MusConvTrack(), token).ConfigureAwait(false);
				var result = await SearchTrack(track, tracks).ConfigureAwait(false);
				return result;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return new MusConvTrackSearchResult(track);
			}
		}

		#endregion TransferMethods

		#region InnerMethods

		private async Task<MusConvTrackSearchResult> SearchTrack(MusConvTrack track, IReadOnlyCollection<MusConvTrack> tracks)
		{
			var foundTracks = new List<MusConvTrack>();
			var title = track.Title.Length < 5 ? track.Title : track.Title[..5];
			var artist = track.Artist.Length < 5 ? track.Artist : track.Artist[..5];
			await Task.Run(() =>
			{
				foundTracks = tracks.Where(t =>
					t.FullName.StartsWith(title, StringComparison.InvariantCultureIgnoreCase)
					|| !string.IsNullOrEmpty(artist)
					&& t.Artist.StartsWith(artist, StringComparison.InvariantCultureIgnoreCase))
					.ToList();
			}).ConfigureAwait(false);

			var sortedTracks = new MusConvTrackSearchResult(track, foundTracks);
			sortedTracks = Sort.Sort.DeleteNotValidTracks(sortedTracks, MainViewModel.SettingsVM);

			return sortedTracks;
		}

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