using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class iHeartRadioViewModel : SectionViewModelBase
	{
		#region Constructors

		public iHeartRadioViewModel(MainViewModelBase m) : base(m)
		{
			Title = "iHeartRadio";
			SourceType = DataSource.IHeartRadio;
			LogoKey = LogoStyleKey.iHeartRadioLogo;
			SmallLogoKey = LogoStyleKey.iHeartRadioLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.iHeartRadioSideLogo;
			CurrentVMType = VmType.Service;
			RegState = RegistrationState.Unlogged;
			CurrentVMType = VmType.Service;
			Model = new iHeartRadioModel();
			BaseUrl = "https://www.iheart.com/";

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
			};

			#endregion Commands

			#region TransferTasks

			var albumTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m)
			};
			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase, 
				new LogOut_TaskItem("LogOut", Log_Out));
			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			MainViewModel.GoBack();
			OnLoginPageLeft();

			await Model.AuthorizeAsync(s as string, t as string).ConfigureAwait(false);

			SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(s.ToString(), t.ToString()) });
			
			UserEmail = s.ToString();

			await InitialAuthorization().ConfigureAwait(false);
		}

		public override async Task<bool> IsServiceSelectedAsync()
		{			
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (Model.IsAuthorized == false)
				{
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
					{
						return false;
					}

					await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
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
			RegState = RegistrationState.Unlogged;
			Model.IsAuthorized = false;
			OnLogoutReceived(new AuthEventArgs(Model));

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				foreach (var t in Tabs)
					t.MediaItems.Clear();
				NavigateToEmailPasswordLoginForm();
			});

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var authData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());		
			await Model.AuthorizeAsync(authData["Login"], authData["Password"]).ConfigureAwait(false);
			UserEmail = authData["Login"];
			await InitialAuthorization();
			return Model.IsAuthorized;
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

			foreach (var pair in result)
			{
				try
				{
					var tracks = pair.Value.Select(x => x.ResultItems.FirstOrDefault());
					var createModel = new MusConvPlaylistCreationRequestModel(pair.Key, tracks);
					var playlist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);

					MainViewModel.ResultVM.MediaItemIds.Add(playlist.Id);
				}
				catch (Exception ex)
				{
					MusConvLogger.LogFiles(ex);
				}
			}
			progressReport.Report(GetPlaylistsReportCount(result));
		}

		#endregion TransferMethods

		#region InnerMethods

		private string GetSerializedServiceData(string s, string t)
		{
			return Serializer.Serialize(new Dictionary<string, string>
								{
									{ "Login", s},
									{ "Password", t},
								});
		}

		#endregion InnerMethods
	}
}