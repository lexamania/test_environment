using System;
using System.Linq;
using System.Threading.Tasks;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Sentry;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class SpinitronViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public SpinitronViewModel(MainViewModelBase m) : base(m)
		{
			Model = new SpinitronModel();
			Title = "Spinitron";
			SourceType = DataSource.Spinitron;
			LogoKey = LogoStyleKey.Spinitron;
			SideLogoKey = LeftSideBarLogoKey.SpinitronSideLogo;
			Url = "https://spinitron.com/";
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
				foreach (var link in URLs)
				{
					try
					{
						SelectedTab.MediaItems.Add(await (Model as SpinitronModel).GetPlaylistAsync(link).ConfigureAwait(false));
					}
					catch
					{
						WrongURLs.Add(link);
						continue;
					}
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