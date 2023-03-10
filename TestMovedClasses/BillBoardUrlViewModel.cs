using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class BillBoardUrlViewModel : WebUrlViewModelBase
	{
		#region Constructors

        public BillBoardUrlViewModel(MainViewModelBase m) : base(m)
        {
            Title = "BillboardUrl";
			Model = new BillBoardUrlModel();
			SourceType = DataSource.BillboardUrl;
            LogoKey = LogoStyleKey.BillboardUrlLogo;
			SideLogoKey = LeftSideBarLogoKey.BillboardSideLogo;
			Url = Urls.Billboard;
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
						SelectedTab.MediaItems.Add(await (Model as BillBoardUrlModel).GetPlaylistAsync(item).ConfigureAwait(false));
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