using Avalonia.Threading;
using MusConv.Lib.TikTokUrl.Exceptions;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class TikTokUrlViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public TikTokUrlViewModel(MainViewModelBase main) : base(main)
		{
			Title = "TikTokUrl";
			Model = new TikTokUrlModel();
			SourceType = DataSource.TikTokUrl;
			LogoKey = LogoStyleKey.TikTokUrlLogo;
			SideLogoKey = LeftSideBarLogoKey.TikTokUrlSideLogo;
			Url = Urls.TikTok;

			#region Commands

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			var tracksTab = new TrackTabViewModelBase(main, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase);

			Tabs.Clear();
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				SelectedTab.Loading = true;
				MainViewModel.NavigateTo(NavigationKeysChild.Content);
				SelectedTab.MediaItems.Clear();

				URLs = s.ToString().Split(Environment.NewLine).Where(x => !string.IsNullOrEmpty(x));
				foreach (var item in URLs)
				{
					MusConvTrack track;
					try
					{
						track = await (Model as TikTokUrlModel).GetTrack(item).ConfigureAwait(false);
					}
					catch (TikTokWithoutMusicException)
                    {
						_urlsWithNoMusic.Add(item);
						continue;
                    }
					catch
					{
						WrongURLs.Add(item);
						continue;
					}
					
					SelectedTab.MediaItems.Add(track);
				}

				await ShowWrongURLs();
				Initial_Setup();
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				await ShowError(Texts.EnterCorrectWebURLs);
				MainViewModel.NavigateTo(NavigationKeysChild.WebUrl);
			}
		}

		#endregion AuthMethods

		#region InnerMethods

		private string GetNoMusicExceptionMessage(List<string> wrongURLs)
        {
			var result = String.Join("\n", wrongURLs.ToArray());
			return $"Found {wrongURLs.Count} URLs with no music \n\nURLs:\n{result}";
		}

		#endregion InnerMethods
	}
}