using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using static MusConv.MessageBoxManager.MessageBox;
using Avalonia.Threading;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	class TraxsourceViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public TraxsourceViewModel(MainViewModelBase m) : base(m)
		{
			Model = new TraxsourceModel();
			Title = "Traxsource";
			SourceType = DataSource.Traxsource;
			LogoKey = LogoStyleKey.TraxsourceLogo;
			SideLogoKey = LeftSideBarLogoKey.TraxsourceSideLogo;
			Url = "https://www.traxsource.com/";
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
					MusConvPlayList playlist;
					try
					{
						playlist = await (Model as TraxsourceModel).GetAlbumAsync(item).ConfigureAwait(false);
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