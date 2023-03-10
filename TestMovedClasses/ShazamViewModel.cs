using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Lib.Shazam.Exceptions;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using PropertyChanged;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.Models;
using System.Threading;
using MusConv.Abstractions.Exceptions;
using MusConv.ViewModels.ViewModels.Base.Commands.View.Base;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.Sentry.Attributes;
using MusConv.ViewModels.Models.ServicesModels;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class FailedProcessLinkException : Exception
	{
		public FailedProcessLinkException(Exception innerException, string link) : base(link, innerException)
		{
			Link = link;
		}

		[SentryEventAdditionalInfo] public string Link { get; }
	}

	[AddINotifyPropertyChangedInterface]
	public class ShazamViewModel : SectionViewModelBase
	{
		#region Fields

		private readonly ShazamModel _shazamModel;
		public bool EmailControlsEnabled => !LinkControlsEnabled;
		public bool LinkControlsEnabled { get; set; }

		#endregion Fields

		#region Constructors

		public ShazamViewModel(MainViewModelBase mainViewModel) : base(mainViewModel)
		{
			Title = "Shazam";
			Model = new ShazamModel();
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Shazam;
			LogoKey = LogoStyleKey.ShazamLogo;
			SmallLogoKey = LogoStyleKey.ShazamLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.ShazamSideLogo;
			CurrentVMType = VmType.Service;
			BaseUrl = "https://www.shazam.com/";

			_shazamModel = Model as ShazamModel;

			#region Commands

			var commandArtistTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewOnCommandBase(Commands.ViewOnShazam, CommandTrack_Open)
			};
			
			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnCommandBase(Commands.ViewOnShazam, CommandTrack_Open)
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnCommandBase(Commands.ViewOnShazam, CommandTrack_Open)
			};

			#endregion Commands

			#region TransferTasks
			
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchTrack, TransferTrack_Send, mainViewModel)
			};

			#endregion TransferTasks

			#region Tabs
			
			var artistsTab = new ArtistTabViewModelBase(mainViewModel, LwTabIconKey.ArtistIcon,
				EmptyTransfersBase, commandArtistTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks);
		
			var tracksTab = new TrackTabViewModelBase(mainViewModel, LwTabIconKey.TrackIcon,
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase);			
			
			var relatedTab = new TrackTabViewModelBase(mainViewModel, AppTabs.YouMightAlsoLike, LwTabIconKey.YouMightAlsoLikeTrackIcon,
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_YouMightAlsoLike), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(tracksTab);
			Tabs.Add(artistsTab);
			Tabs.Add(relatedTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			MainViewModel.NeedLogin = this;
			
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			
			if (!Model.IsAuthorized)
			{
				if (SaveLoadCreds.IsCredsExists(out var data) && await IsServiceDataExecuted(data))
					return false;
				
				MainViewModel.NavigateTo(NavigationKeysChild.EmailPasswordLoginForm);

				return false;
			}

			(Model as ShazamModel)!.AllFoundedTracksId.Clear();
			
			await InitialUpdateForCurrentTab().ConfigureAwait(false);
			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = Serializer.Deserialize<ShazamUser>(data[Title].FirstOrDefault());

			_tempUserCredentials.email = serviceData.Email;

			await ProcessLink_Command(_shazamModel.GetValidateEmailUrl(serviceData.AccessToken));

			return true;
		}

		public override async Task<bool> Log_Out(bool arg)
		{
			RemoveUserCredentials();
			
			Model.IsAuthorized = false;
			RegState = RegistrationState.Unlogged;
			(Model as ShazamModel)!.AllFoundedTracksId.Clear();

			MainViewModel.NavigateTo(NavigationKeysChild.EmailPasswordLoginForm);

			return true;
		}

		public Task OnFailedAuthorize(string error)
		{
			MusConvLogger.LogFiles(new ShazamException(error));

			RemoveUserCredentials();

			(Model as ShazamModel)!.AllFoundedTracksId.Clear();
			Model.IsAuthorized = false;

			return Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
		}

		public async Task OnSuccessAuthorization()
		{
			if (Model.IsAuthorized) return;

			SaveUserCredentials(new ShazamUser { AccessToken = _tempUserCredentials.accessToken, Email = _tempUserCredentials.email });

			UserEmail = _tempUserCredentials.email;
			Model.AccessToken = _tempUserCredentials.accessToken;
			Model.IsAuthorized = true;
			RegState = RegistrationState.Logged;
			_tempUserCredentials = default;

			await Model.Initialize();
		}

		#endregion AuthMethods

		#region Commands

		public Task Command_Help(object arg1)
		{
			return Command_Help(arg1, CommandTaskType.CommandBar);
		}

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			MainViewModel.NavigateTo(NavigationKeysChild.ShazamHelp);
			return Task.CompletedTask;
		}

		public override async Task Command_Reload(object arg1, CommandTaskType commandTaskType)
		{
			(Model as ShazamModel)!.AllFoundedTracksId.Clear();

			await ProcessLink_Command(_shazamModel.GetValidateEmailUrl(Model.AccessToken));
		}

		public async Task NavigateToLoginGuide_Command() =>
			MainViewModel.NavigateTo(NavigationKeysChild.HowToLoginIntoShazamHelpPage);

		public async Task ProcessEmail_Command(string email)
		{
			try
			{
				if (string.IsNullOrEmpty(email))
				{
					await ShowError(Texts.EmailEmpty);
					return;
				}

				await Model.SendEmailAsync(email);

				_tempUserCredentials.email = email;

				LinkControlsEnabled = true;
			}
			catch (FailedHttpRequestException ex) when (ex.Response.StatusCode == HttpStatusCode.TooManyRequests)
			{
				MusConvLogger.LogFiles(ex);

				await Log_Out(true);
				await ShowMessage("You can receive no more than 5 messages per day");
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);

				await ShowError(Texts.IncorrectCredentials);
			}
		}

		public async Task ProcessLink_Command(string link)
		{
			try
			{
				if (string.IsNullOrEmpty(link))
				{
					await ShowError(Texts.EmailEmpty);

					return;
				}

				LinkControlsEnabled = false;

				Url = link;

				_tempUserCredentials.accessToken = _shazamModel.GetCustomTokenFromUrl(link);

				NavigateToBrowserLoginPage();
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(new FailedProcessLinkException(ex, link));

				await ShowError(Texts.ShazamFailedToProcessLinkMessage);
			}
		}

		#endregion Commands

		#region TransferMethods

		public override async Task Transfer_SaveInTo(object[] items)
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

		#endregion TransferMethods

		#region InnerMethods

		public async Task<MusConvTrackSearchResult> Transfer_SearchTrack(
			int index,
			MusConvTrack track,
			IProgress<ReportCount> progress,
			CancellationToken token)
		{
			try
			{
				var search = $"\"{track.Title}\" by \"{track.Artist}\"";
				progress.Report(new ReportCount(index, $"Searching: {search} ", ReportType.Searching));

				token.ThrowIfCancellationRequested();
				var result = await DefaultSearchAsync(track, token);

				return result;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return new MusConvTrackSearchResult(track);
			}
		}

		private void SaveUserCredentials(ShazamUser shazamUser) => 
			SaveLoadCreds.SaveData(new List<string> { Serializer.Serialize(shazamUser) });

		private void RemoveUserCredentials() => 
			SaveLoadCreds.DeleteServiceData();

		#endregion InnerMethods
	}
}