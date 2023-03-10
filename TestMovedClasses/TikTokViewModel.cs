using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusConv.ViewModels.Models;
using System;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class TikTokViewModel : SectionViewModelBase
	{
		#region Fields

		private bool isAuthenticated;

		#endregion Fields

		#region Constructors

		public TikTokViewModel(MainViewModelBase main) : base(main)
		{
			Title = "TikTok";
			CurrentVMType = VmType.Service;
			SourceType = DataSource.TikTok;
			LogoKey = LogoStyleKey.TikTokLogo;
			SmallLogoKey = LogoStyleKey.TikTokLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.TikTokSideLogo;
			RegState = RegistrationState.Needless;
			Model = new TikTokModel();

			var commandTracksTab = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			//https://tokboard.com/
			var tikTokTab = new TrackTabViewModelBase(main, "Weekly TikTok", LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_TikTokWeekly), EmptyCommandsBase);

			Tabs.Add(tikTokTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				NavigateToContent();
				if (!isAuthenticated)
				{
					SelectedTab.Loading = true;
				}
				isAuthenticated = true;
				return await Initial_Setup().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}


			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

		public async Task<bool> Initial_Setup()
		{
			var res = await Initial_Update_TikTokWeekly();
			return res;
		}

		private async Task<bool> Initial_Update_TikTokWeekly(bool forceUpdate = false)
		{
			var tab = SelectedTab;

			SelectedTab.LoadingText = MusConvConfig.PlayListLoading;

			await Dispatcher.UIThread.InvokeAsync(() => tab.MediaItems.Clear());

			if(listoftracks.Count == 0)
			{
				var Items = await (Model as TikTokModel).ParseTikTok().ConfigureAwait(false);
				foreach (MusConvPlayList playlist in Items)
				{
					foreach (MusConvTrack track in playlist.AllTracks)
					{
						listoftracks.Add(track);
					}
				}
			}
			tab.MediaItems?.Clear();
			await Dispatcher.UIThread.InvokeAsync(() => tab.MediaItems.AddRange(listoftracks));
			SelectedTab.Loading = false;
			return true;
		}

		#endregion InitialUpdate
	}
}