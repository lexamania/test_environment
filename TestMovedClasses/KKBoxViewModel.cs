using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Threading;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Sentry;
using MusConv.ViewModels.Models.MusicService;
using MusConv.MessageBoxManager.Enums;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class KKBoxViewModel : SectionViewModelBase
	{
		#region Constructors

		public KKBoxViewModel(MainViewModelBase m) : base(m)
		{
			Title = "KKBox";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.KKBox;
			LogoKey = LogoStyleKey.KKBoxLogo;
			SideLogoKey = LeftSideBarLogoKey.KKBoxSideLogo;
			CurrentVMType = VmType.Service;
			BaseUrl = "https://www.kkbox.com/intl/index.php?area=intl";

			Model = new KKBoxModel();

			Url = Model.Url;

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks);
			var playlistFeaturedTab = new PlaylistTabViewModelBase(m, TabsKey.Featured.ToString(), LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Featured), commandTracks);

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(playlistFeaturedTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task SelectServiceAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (!Model.IsAuthorized)
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				}
				else
				{
					await InitialUpdateForCurrentTab().ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				Initial_Setup();
			}
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				await Model.AuthorizeAsync(s as string, null).ConfigureAwait(false);

				if (!Model.IsAuthorized)
				{
					await ShowMessage("Authorization failed", Icon.Error);
					return;
				}

				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				RegState = RegistrationState.Logged;
				if (IsSending)
				{
					WaitAuthentication.Set();
					IsSending = false;
				}
				else
				{
					await Initial_Update_Playlists().ConfigureAwait(false);
				}
				Initial_Setup();
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				Debug.WriteLine(e);
			}
		}

		#endregion AuthMethods
	}
}