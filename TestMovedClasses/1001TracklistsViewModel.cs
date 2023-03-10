using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	class _1001TracklistsViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public _1001TracklistsViewModel(MainViewModelBase m) : base(m)
		{
			Model = new _1001TracklistsModel();
			Title = "1001Tracklists";
			SourceType = DataSource._1001Tracklists;
			LogoKey = LogoStyleKey._1001TracklistsLogo;
			SideLogoKey = LeftSideBarLogoKey._1001TracklistsSideLogo;
			Url = "https://www.1001tracklists.com/";
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			SelectedTab.Loading = true;
			try
			{
				NavigateToContent();
				SelectedTab.MediaItems.Clear();

				URLs = s.ToString().Split(Environment.NewLine).Where(x => !string.IsNullOrEmpty(x));
				foreach (var item in URLs)
				{
					try
					{
						SelectedTab.MediaItems.Add(await (Model as _1001TracklistsModel).GetPlaylistAsync(item).ConfigureAwait(false));
					}
					catch
					{
						WrongURLs.Add(item);
						continue;
					}
				}

				await ShowWrongURLs();
				SelectedTab.Loading = false;
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