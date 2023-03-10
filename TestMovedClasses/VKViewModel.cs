using Avalonia.Threading;
using Flurl.Http;
using MusConv.Abstractions;
using MusConv.Abstractions.Extensions;
using MusConv.Lib.VkMusic.Exceptions;
using MusConv.MessageBoxManager.Enums;
using MusConv.MessageBoxManager.Texts;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VkNet.Exception;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class VKViewModel : SectionViewModelBase
	{
		#region Fields

		private string _temporarylogin;
		private string _temporaryPassword;
		public bool LogOut { get; set; }
		public bool Refreshing { get; set; }

		#endregion Fields

		#region Constructors

		public VKViewModel(MainViewModelBase main) : base(main)
		{
			Title = "VK Music (VKontakte)";
			BaseUrl = "https://vk.com/music/";
			SourceType = DataSource.VK;
			LogoKey = LogoStyleKey.VKLogo;
			SideLogoKey = LeftSideBarLogoKey.VKSideLogo;
			CurrentVMType = VmType.Service;
			RegState = RegistrationState.Unlogged;
			LoginPassPageFirstField = "Email or phone number";

			var model = new VKModel();
			Model = model;
			Url = model.Api.OAuthUrl;

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
				new EditCommand(Command_Edit, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
				new SplitCommand(Command_Split, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
				new MergePlaylistsCommand(Command_Merge, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.CommandBar),

				new ViewOnVkMusicCommand(CommandTrack_Open),
				new EditCommand(Command_Edit, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
				new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
				new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.DropDownMenu),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),

				new ViewOnVkMusicCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),

				new ViewOnVkMusicCommand(CommandTrack_Open),
				new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
			};

			var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),

				new ViewOnVkMusicCommand(CommandTrack_Open),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),

				new ViewOnVkMusicCommand(CommandTrack_Open),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
			};

			#endregion Commands

			#region Transfer

			var transferPlaylists = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, main)
			};
			var transferTracks = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, main)
			};

			#endregion Transfer

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(main, LwTabIconKey.PlaylistIcon,
				transferPlaylists, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandPlaylistTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(main, LwTabIconKey.AlbumIcon,
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(main, "My music", LwTabIconKey.TrackIcon,
				transferTracks, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var topTracksTab = new TrackTabViewModelBase(main, "Top tracks", LwTabIconKey.TrackIcon,
				transferTracks, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Particular_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(tracksTab);
			Tabs.Add(topTracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (Model.IsAuthorized)
				{
					await InitialUpdateForCurrentTab().ConfigureAwait(false);
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

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				OnLoginPageLeft();

				if (s == null || t == null)
				{
					await IsServiceSelectedAsync().ConfigureAwait(false);
					return;
				}

				if (t is VkAuthType authType && s is VkUserCredential userCredential)
				{
					switch (authType)
					{
						case VkAuthType.Token:
							await AuthorizeWithTokenAndUserId(userCredential.AccessToken, userCredential.UserId);
							break;
						case VkAuthType.Credentials:
							await AuthorizeWithLoginAndPassword(userCredential.Login, userCredential.Password);
							break;
					}
				}
				else if (t is TypeOfObjectToTransmit.CodeFromSMS)
				{
					await AuthorizeWithLoginAndPasswordAndAuthCode(_temporarylogin, _temporaryPassword, s.ToString());
				}
				else
				{
					// but refresh token method dont worked (maybe)
					await AuthorizeWithLoginAndPassword(s.ToString(), t.ToString());
				}

				if (!Model.IsAuthorized)
				{
					await ShowMessage("Authorization failed", Icon.Error);
					return;
				}

				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				await InitialAuthorization();

				_temporarylogin = string.Empty;
				_temporaryPassword = string.Empty;
			}
			catch (TwoFactorAuthException)
			{
				_temporarylogin = s!.ToString();
				_temporaryPassword = t!.ToString();

				NavigateToSendTwoAuthCodePage();
			}
			catch (FlurlHttpException ex) when (ex.Message.Contains(
				"Call failed. A connection attempt failed because the connected party did not properly respond after a period of time"))
			{
				await ShowMessage(Texts.RecommendationToUseVpn, Icon.Error);
			}
			catch (VkUserUnauthorizedException)
			{
				NavigateToEmailPasswordLoginForm();

				await ShowMessage(Texts.AuthorizationFailed + "\nPlease, try again!", Icon.Error);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				await Log_Out();
				await ShowMessage(ex.Message, Icon.Error);
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
			else if (!Refreshing)
			{
				await InitialUpdateForCurrentTab(true).ConfigureAwait(false);
				OnAuthReceived(new AuthEventArgs(Model));
			}
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<VkUserCredential>(data[Title].FirstOrDefault());
			await Web_NavigatingAsync(serviceData, VkAuthType.Token);
			return Model.IsAuthorized;
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			await Model.Logout().ConfigureAwait(false);
			SaveLoadCreds.DeleteServiceData();
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));
			Refreshing = false;
			LogOut = true;
			await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
			return true;
		}

		private async Task AuthorizeWithLoginAndPassword(string login, string password)
		{
			var model = Model as VKModel;
			await model.AuthorizeCredAsync(login, password).ConfigureAwait(false);
			SaveCurrentCredentials(await model.GetUserIdentifier(), login, password);
		}

		private async Task AuthorizeWithLoginAndPasswordAndAuthCode(string login, string password, string authCode)
		{
			var model = Model as VKModel;
			await model.AuthorizeCredAndTwoAuthCodeAsync(login, password, authCode).ConfigureAwait(false);
			SaveCurrentCredentials(await model.GetUserIdentifier(), login, password);
		}

		private async Task AuthorizeWithTokenAndUserId(string accessToken, string userId)
		{
			await Model.AuthorizeAsync(accessToken, userId).ConfigureAwait(false);
			SaveCurrentCredentials(userId);
		}

		#endregion AuthMethods

		#region InitialUpdate

        public override Task<bool> Initial_Update_Playlists(bool forceUpdate = false)
		{
			return Initial_Update_Playlists_DetailedLoading(forceUpdate);
		}

		public override Task<bool> Initial_Update_Album(bool forceUpdate = false)
		{
			return Initial_Update_Album_DetailedLoading(forceUpdate);
		}

		public override async Task<bool> InitialUpdateBuilder<T>(Func<Task<T>> getMethod, TabViewModelBase selectedTab, bool forceUpdate = false, bool overrideException = false)
		{
			try
			{
				return await base.InitialUpdateBuilder(getMethod, selectedTab, forceUpdate, overrideException: true);
			}
			catch (VkUserUnauthorizedException)
			{
				await Log_Out(true);
				await ShowError("Failed to authorize user");
			}
			catch (ArgumentException ex) when (ex.Message.Contains("An item with the same key has already been added"))
			{
				MusConvLogger.LogFiles(ex);
				await Command_Reload(null, CommandTaskType.CommandBar);
			}
			catch (UnknownMethodException)
			{
				await Log_Out(true);
				await ShowHelp(MessageBoxText.VkCannotGetAccountDetails);
			}
			catch (FlurlHttpTimeoutException)
			{
				await ShowMessage(Texts.VkRecommendationToUserVpn, Icon.Info);
			}
			catch (Exception ex)
			{
				await Log_Out(true);
				ShowUnableToLoadTabDependant(ex);
				MusConvLogger.LogFiles(ex);
			}

			return false;
		}

		#endregion InitialUpdate

		#region Commands

        public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			return ShowHelp(MessageBoxText.VkMusicHelp);
		}

		#endregion Commands

		#region TransferMethods

		public override async Task Transfer_SaveInTo(object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);

			IsSending = true;

			if (!Model.IsAuthorized)
			{
				if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) || !await IsServiceDataExecuted(data))
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

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);

			foreach (var resultKey in result.Keys)
			{
				try
				{
					var tracks = result[resultKey].Select(x => x.ResultItems?.FirstOrDefault())
						.AllIsNotNull().ToList();
					var playlistRequest = new MusConvPlaylistCreationRequestModel(resultKey, tracks);
					token.ThrowIfCancellationRequested();
					var report = new ReportCount(resultKey.AllTracks.Count, 
						$"Adding tracks to playlist \"{resultKey}\", please wait", ReportType.Sending);
					await Dispatcher.UIThread.InvokeAsync(() => progressReport.Report(report));
					await Model.CreatePlaylist(playlistRequest).ConfigureAwait(false);
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

		private void SaveCurrentCredentials(string userId, string login = null, string password = null)
		{
			var credentials = new VkUserCredential
			{
				UserId = userId, 
				Login = login, 
				Password = password, 
				AccessToken = (Model as VKModel).Api.GetAccessToken()
			};

			SaveLoadCreds.SaveData(new List<string> { Serializer.Serialize(credentials) });
		}

		#endregion InnerMethods
	}
	
	internal class VkUserCredential
	{
		public string Login { get; set; }
		public string Password { get; set; }   
		public string AccessToken { get; set; }
		public string UserId { get; set; }
	}

	internal enum VkAuthType
	{
		Token,
		Credentials,
		TwoAuthCode
	}
}