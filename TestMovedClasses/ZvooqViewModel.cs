using Avalonia.Threading;
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
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Abstractions.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class ZvooqViewModel : SectionViewModelBase
	{
		#region Constructors

		public ZvooqViewModel(MainViewModelBase main) : base(main)
		{
			Title = "Zvooq";
			LogoKey = LogoStyleKey.ZvooqLogo;
			SideLogoKey = LeftSideBarLogoKey.ZvooqSideLogo;
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Zvooq;
			CurrentVMType = VmType.Service;
			BaseUrl = "https://zvooq.com/";

			Model = new ZvooqModel();

			#region Commands

			var commandPlaylist = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTrack = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			var commandTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, main)
			};

			var playlistsTab = new PlaylistTabViewModelBase(main, LwTabIconKey.PlaylistIcon, 
				commandTransfer, commandPlaylist,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTrack, 
				new LogOut_TaskItem("LogOut", Log_Out));

			Tabs.Add(playlistsTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			MainViewModel.NeedLogin = this;
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

			if (!Model.IsAuthorized)
				await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
			else
			{
				await InitialUpdateForCurrentTab().ConfigureAwait(false);
			}

			return false;
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			await Model.AuthorizeAsync(s.ToString(), t.ToString()).ConfigureAwait(false);

			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			UserEmail = s.ToString();
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

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				ClearAllMediaItems();
				NavigateToEmailPasswordLoginForm();
			});

			return true;
		}

		#endregion AuthMethods

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			IsSending = true;

			if (!Model.IsAuthorized)
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
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
			int i = 0;
			foreach (var resultKey in result.Keys)
			{ 
				var createModel = new MusConvPlaylistCreationRequestModel(resultKey);
				var createdPlaylist = await Model.CreatePlaylist(createModel).ConfigureAwait(false);

				progressReport.Report(new ReportCount(0, $"Adding tracks to playlist \"{resultKey}\", please wait",
					ReportType.Sending));

				foreach (var searchResultList in result[resultKey].Where(y => y.ResultItems?.Count != 0).ToList().SplitList().ToList())
				{
					token.ThrowIfCancellationRequested();

					var tracks = searchResultList.Where(t => t.ResultItems != null)
						.Select(t => t.ResultItems.FirstOrDefault()).ToList();
					try
					{
						await Model.AddTracksToPlaylist(createdPlaylist, tracks).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						MusConvLogger.LogFiles(ex);
					}

					progressReport.Report(new ReportCount(tracks.Count,
						$"Adding tracks to playlist \"{resultKey}\", please wait",
						ReportType.Sending));
				}
				i++;
			}

			progressReport.Report(GetPlaylistsReportCount(result));
		}

		#endregion TransferMethods
	}
}