using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.Models;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	class RollingStoneViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public RollingStoneViewModel(MainViewModelBase m) : base(m)
		{
			Model = new RollingStoneModel();
			Title = "RollingStone";
			SourceType = DataSource.RollingStone;
			LogoKey = LogoStyleKey.RollingStoneLogo;
			SideLogoKey = LeftSideBarLogoKey.RollingStoneSideLogo;
			Url = Urls.RollingStone + "/music/music-lists";
			BaseUrl = Urls.RollingStone;
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

				URLs = s.ToString().Split('\n').Where(x => !string.IsNullOrEmpty(x));
				foreach (var item in URLs)
				{
					MusConvPlayList playlist;
					try
					{
						playlist = await (Model as RollingStoneModel).GetPlaylistAsync(item.ToString()).ConfigureAwait(false);
					}
					catch
					{
						WrongURLs.Add(item);
						continue;
					}
					
					SelectedTab.MediaItems.Add(playlist);
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
	}
}