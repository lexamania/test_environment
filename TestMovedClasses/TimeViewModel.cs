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
	class TimeViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public TimeViewModel(MainViewModelBase m) : base(m)
		{
			Model = new TimeModel();
			Title = "Time";
			SourceType = DataSource.Time;
			LogoKey = LogoStyleKey.TimeLogo;
			SideLogoKey = LeftSideBarLogoKey.TimeSideLogo;
			Url = "https://time.com/";
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
						SelectedTab.MediaItems.Add(await (Model as TimeModel).GetPlaylistAsync(item).ConfigureAwait(false));
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