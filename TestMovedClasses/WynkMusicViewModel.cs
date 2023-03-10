using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Lib.WynkMusic.Model;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MusConv.MessageBoxManager.Texts;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry.Extensions;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class WynkMusicViewModel : SectionViewModelBase
	{
		#region Fields

		private string phoneNumber;

		#endregion Fields

		#region Constructors

		public WynkMusicViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Wynk Music";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.WynkMusic;
			LogoKey = LogoStyleKey.WynkMusicLogo;
			SideLogoKey = LeftSideBarLogoKey.WynkMusicSideLogo;
			CurrentVMType = VmType.Service;
			Model = new WynkMusicModel();
			Url = "https://wynk.in/music";

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
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
			var artistsTransfer = new List<TaskBase_TaskItem>
			{
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out))
			{
				DefaultLoadingText = "Loading playlist..., it may take a few minutes..."
			};

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistsTransfer, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out))
			{
				DefaultLoadingText = "Loading artists..., it may take a few minutes..."
			};

			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab, 
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out))
			{
				DefaultLoadingText = "Loading tracks..., it may take a few minutes..."
			};

			#endregion Tabs

			Tabs.Add(playlistTab);
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

				if (!Model.IsAuthenticated())
				{
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
					{
						return false;
					}

					await Dispatcher.UIThread.InvokeAsync(() => MainViewModel.NavigateTo(NavigationKeysChild.SendPhoneNumber));
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
			try
			{
				switch ((int)t)
				{
					case (int)TypeOfObjectToTransmit.PhoneNumber:
						await Model.SendPhoneNumberAsync(s as string).ConfigureAwait(false);
						phoneNumber = s as string;
						break;
					case (int)TypeOfObjectToTransmit.CodeFromSMS:
						var userInfo = await Model.AuthorizeAsync(phoneNumber, s).ConfigureAwait(false);
						await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
						RegState = RegistrationState.Logged;

						SaveLoadCreds.SaveData(new List<string> { GetSerializedServiceData(userInfo) });

						if (IsSending)
						{
							WaitAuthentication.Set();
							IsSending = false;
						}
						else
						{
							await Initial_Update_Playlist().ConfigureAwait(false);
						}
						Initial_Setup();
						break;
				}
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message, ButtonEnum.RightOk);
				MusConvLogger.LogFiles(sentryEvent);
				Debug.WriteLine(e);
			}
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				ClearAllMediaItems();
				MainViewModel.NavigateTo(NavigationKeysChild.SendPhoneNumber);
			});

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var userInfo = Serializer.Deserialize<UserInfo>(data[Title].FirstOrDefault());
			(Model as WynkMusicModel).Client = new Lib.WynkMusic.Client(userInfo);
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			RegState = RegistrationState.Logged;
			if (IsSending)
			{
				WaitAuthentication.Set();
				IsSending = false;
			}
			else
			{
				await Initial_Update_Playlist().ConfigureAwait(false);
			}
			Initial_Setup();

			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

		private async Task Initial_Update_Playlist(bool forceUpdate = false)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);

			SelectedTab.Loading = true;

			try
			{
				if (SelectedTab == null || SelectedTab.MediaItems == null || SelectedTab.MediaItems.Count == 0 || forceUpdate)
				{
					var items = await Model.GetPlaylists().ConfigureAwait(false);

					if (items != null)
					{
						SelectedTab.MediaItems?.Clear();
						SelectedTab.MediaItems.AddRange(items);
					}
				}
			}
			catch (Exception ex)
			{
				UnableToLoadPlaylists(ex);
				MusConvLogger.LogFiles(ex);
				Initial_Setup();
			}
			
			TabsEnabled = true;

			Initial_Setup();
		}

		#endregion InitialUpdate

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			ShowHelp(MessageBoxText.WynkHelp);

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
					await Dispatcher.UIThread.InvokeAsync(() => MainViewModel.NavigateTo(NavigationKeysChild.SendPhoneNumber));
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

			foreach (var resultKey in result.Keys)
			{
				try
				{
					var tracks = result[resultKey]
						.Where(x => x.ResultItems.Count != 0)
						.Select(x => x.ResultItems.FirstOrDefault());
					var id = await Model.CreatePlaylist(new MusConvPlaylistCreationRequestModel(resultKey, tracks)).ConfigureAwait(false);
					MainViewModel.ResultVM.MediaItemIds.Add(id.ToString());
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

		private string GetSerializedServiceData(object userInfo)
		{
			return Serializer.Serialize(userInfo);
		}

		#endregion InnerMethods
	}
}