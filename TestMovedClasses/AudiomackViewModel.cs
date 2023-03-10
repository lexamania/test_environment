using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Abstractions.Extensions;
using MusConv.Lib.Audiomack;
using MusConv.Sentry;
using MusConv.Sentry.Extensions;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.EventArguments;
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
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class AudiomackViewModel:SectionViewModelBase
	{
		#region Fields

		public AuthorizationResponse AuthData { get; private set; }
		public static Audiomack Audiomack { get; private set; }
		private AudiomackModel CurrentModel { get; set; }

		#endregion Fields

		#region AuthMethods

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

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				if (s == null) return;
				var login = ((string)s).Replace("\n", string.Empty);
				var password = ((string)t).Replace("\n", string.Empty);
				AuthData = await new Audiomack().AuthorizeTask(login, password).ConfigureAwait(false);
				CurrentModel.AuthData = AuthData;

				SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(login, password) });

				NavigateToContent();
				UserEmail = s.ToString();
				OnLoginPageLeft();

				await InitialAuthorization().ConfigureAwait(false);
				
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
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
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			OnLogoutReceived(new AuthEventArgs(Model));
			ClearAllMediaItems();
			await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var authData = Serializer.Deserialize<AudiomackCredentials>(data[Title].FirstOrDefault());

			AuthData = await new Audiomack().AuthorizeTask(authData.Login, authData.Password).ConfigureAwait(false);
			CurrentModel.AuthData = AuthData;

			UserEmail = authData.Login;
			await InitialAuthorization().ConfigureAwait(false);
			return Model.IsAuthorized;
		}

		#endregion AuthMethods

		#region TransferMethods

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index, IProgress<ReportCount> progressReport, CancellationToken token)
		{
			MainViewModel.ResultVM.SetPlaylistSearchItem(result);
			
			foreach (var resultKey in result.Keys)
			{
				var lt = result[resultKey].ToList().SplitList(50).ToList();
				if (lt?.FirstOrDefault()?.FirstOrDefault()?.ResultItems?.Count > 0 && !string.IsNullOrEmpty(lt.First().First().ResultItems.First().Id))
				{
					var resp = await Audiomack.CreatePlaylistTask(resultKey.Title, "pop", false, AuthData.TokenData).ConfigureAwait(false);
					lt = result[resultKey].ToList().SplitList().ToList();
					foreach (var tracks in lt)
					{
						token.ThrowIfCancellationRequested();
						progressReport.Report(new ReportCount(index, $"Adding \"{tracks.FirstOrDefault()?.OriginalSearchItem?.Title}\" by \"{tracks.FirstOrDefault()?.OriginalSearchItem?.Artist}\" to playlist",
							ReportType.Sending));
						foreach (var track in tracks.Where(x => x.ResultItems != null && x.ResultItems.Count > 0))
						{
							try
							{
								await Audiomack.AddSongToPlaylistTask(resp.Id, track.ResultItems.First().Id, AuthData.TokenData).ConfigureAwait(false);
							}
							catch (Exception ex)
							{
								MusConvLogger.LogFiles(ex);
							}
						}
					}
				}
			}
			
			progressReport.Report(GetPlaylistsReportCount(result));
		}

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthenticated())
			{
				NavigateToEmailPasswordLoginForm();
			}
			else
			{
				NavigateToContent();
				WaitAuthentication.Set();
				IsSending = false;
			}
			await Transfer_DoWork(items[0]).ConfigureAwait(false);
		}

		#endregion TransferMethods

		#region InnerMethods

		public AudiomackViewModel(MainViewModelBase main) : base(main)
		{
			Title = "Audiomack";
			SourceType = DataSource.Audiomack;
			CurrentVMType = VmType.Service;
			LogoKey = LogoStyleKey.AudiomackLogo;
			SideLogoKey = LeftSideBarLogoKey.AudiomackSideLogo;
			RegState = RegistrationState.Unlogged;

			CurrentModel = new AudiomackModel();
			Model = CurrentModel;

			Audiomack = new Audiomack();
			BaseUrl = "https://audiomack.com/";

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			var commandPlaylistTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, main)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, main, true)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(main, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistTab, 
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(main, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(tracksTab);
		}

		private string GetSerializedServiceData(string login, string password)
		{
			return Serializer.Serialize(new AudiomackCredentials { Login = login, Password = password });
		}

		#endregion InnerMethods
	}
}