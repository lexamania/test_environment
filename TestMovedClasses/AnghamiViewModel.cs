using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MusConv.Abstractions;
using MusConv.Lib.Anghami.Models;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.Models;
using System.Threading;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.Abstractions.Extensions;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.Settings;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class AnghamiViewModel : SectionViewModelBase
	{
		#region Fields

		private bool _isAuth;

		#endregion Fields

		#region Constructors

		public AnghamiViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Anghami";
			Model = new AnghamiModel();
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Anghami;
			Url = Urls.Anghami;
			CurrentVMType = VmType.Service;
			LogoKey = LogoStyleKey.AnghamiLogo;
			SideLogoKey = LeftSideBarLogoKey.AnghamiSideLogo;
			IsMultipleAccountsSupported = true;
			ArtistDirectUrl = "https://play.anghami.com/artist/";
			AlbumDirectUrl = "https://play.anghami.com/album/";
			SearchUrl = "https://play.anghami.com/search/";

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
				new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{ 
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m) 
			};
			var albumTransfer = new List<TaskBase_TaskItem>
			{ 
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m) 
			};
			var artistsTransfer = new List<TaskBase_TaskItem>
			{ 
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist,TransferArtist_Search, TransferArtist_Send, m) 
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send, m, true)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", InitialUpdatePlaylists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", InitialUpdateAlbum), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistsTransfer, commandArtistsTab,
				new Initial_TaskItem("Reload", InitialUpdateArtists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", InitialUpdateTracks), EmptyCommandsBase,
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
			if (!_isAuth)
			{
				_isAuth = true;

				var credentials = new AnghamiCredentials(s as string, t as List<Cookie>);
				await Model.Initialize(credentials);

				var serializedCredentials = Serializer.Serialize(credentials);
				SaveLoadCreds.SaveData(new List<string>() { serializedCredentials });

				await InitialAuthorization();
			}
		}

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				MainViewModel.NeedLogin = this;

				if (!Model.IsAuthenticated())
				{
					if (SaveLoadCreds.IsCredsExists(out var data) && await IsServiceDataExecuted(data))
					{
						return true;
					}

					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				}
				else
				{
					await InitialUpdateForCurrentTab().ConfigureAwait(false);
				}
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				Initial_Setup();
				return false;
			}

			return true;
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

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			List<string> serviceData;
			AnghamiCredentials credentials;

			try 
			{
				serviceData = data.FirstOrDefault(x => x.Key == Title).Value;
				credentials = Serializer.Deserialize<AnghamiCredentials>(serviceData.FirstOrDefault());
				await Model.Initialize(credentials);
			}
			catch(Exception e)
			{
				return false;
			}
			
			if (!await Model.IsSavedAuthDataValid())
			{
				if (SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.LogOutOnLogInError))
				{
					await Log_Out();
				}
				else
				{
					await ShowAuthorizationError();
				}          
				return false;
			}

			if (Accounts.Count == 0)
			{
				await LoadUserAccountInfo(serviceData).ConfigureAwait(false);
			}

			await InitialAuthorization();
			return true;
		}

		public override Task LoadUserAccountInfo(List<string> data)
		{
			foreach (var accountData in data)
			{
				var credentials = Serializer.Deserialize<AnghamiCredentials>(accountData);

				var newModel = new AnghamiModel();
				newModel.Initialize(credentials);

				
				var accInfo = new AccountInfo()
				{
					Creds = Serializer.Serialize(new Dictionary<string, string>
				{
					{ "AuthToken", credentials.Token },
					{ "UserId", newModel.Email }
				}),
					Name = newModel.Email,
					Cookies = credentials.Cookies
				};

				Accounts.Add(newModel, accInfo);
			}

			return Task.CompletedTask;
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			_isAuth = false;
			await Model.Logout().ConfigureAwait(false);
			SaveLoadCreds.DeleteServiceData();
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));
			LogOutRequired = true;
			ClearAllMediaItems();
			await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);

			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

		public Task InitialUpdatePlaylists(bool forceUpdate = false)
		{
			return HandleReauthorizationErrorsAsync(() => base.Initial_Update_Playlists());
		}

		public Task InitialUpdateAlbum(bool forceUpdate = false)
		{
			return HandleReauthorizationErrorsAsync(() => Initial_Update_Album());
		}

		public Task InitialUpdateTracks(bool forceUpdate = false)
		{
			return HandleReauthorizationErrorsAsync(() => Initial_Update_Tracks());
		}

		public Task InitialUpdateArtists(bool forceUpdate = false)
		{
			return HandleReauthorizationErrorsAsync(() => Initial_Update_Artists());
		}

		#endregion InitialUpdate

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			try
			{
				if (IsNeedToAddNewAccount || ModelTransferTo is null)
				{
					IsNeedToAddNewAccount = false;
					NavigateToBrowserLoginPage();
				}
				else if (!await IsAccountAuthenticated(ModelTransferTo, Accounts[ModelTransferTo]))
				{
					NavigateToBrowserLoginPage();
				}
				else
				{
					WaitAuthentication.Set();
					IsSending = false;
				}

				await Transfer_DoWork(items[0]).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);
			var indexor = 0;
			try
			{
				foreach (var resultKey in result.Keys)
				{
					var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
					var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false); //Создаем плейлист
					MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

					foreach (var item in result[resultKey].Where(t => t?.ResultItems?.Count > 0)
										.Select(x => x.ResultItems?.FirstOrDefault()).ToList()
										.SplitList())
					{
						token.ThrowIfCancellationRequested();
						await Model.AddTracksToPlaylist(createdPlaylist, item).ConfigureAwait(false);

						await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(new ReportCount(item.Count,
						$"Adding \"{result[resultKey][indexor++].OriginalSearchItem.Title}\" to playlist \"{resultKey}\" ",
						ReportType.Sending)));
					}

					indexor = 0;
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}

			await Dispatcher.UIThread.InvokeAsync(() =>
				progressReport.Report(GetPlaylistsReportCount(result)));
		}

		#endregion TransferMethods

		#region InnerMethods

		public async Task HandleReauthorizationErrorsAsync(Func<Task<bool>> func)
		{
			try
			{
				await func().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (ex.Message == "Wrong credentials,reauthorize")
				{
					LogOutRequired = true;
					await IsServiceSelectedAsync().ConfigureAwait(false);
				}
				else
				{
					ShowUnableToLoadTabDependant(ex);
					MusConvLogger.LogFiles(ex);
				}
			}
		}

		#endregion InnerMethods
	}
}