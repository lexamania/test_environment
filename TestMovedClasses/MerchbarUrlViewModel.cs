using Avalonia.Threading;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
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
    public class MerchbarUrlViewModel : WebUrlViewModelBase
	{
		#region Fields

		private MerchbarUrlModel MerchbarUrlModel => Model as MerchbarUrlModel;

		#endregion Fields

		#region Constructors

		public MerchbarUrlViewModel(MainViewModelBase main) : base(main)
		{
			Title = "MerchbarUr";
			Model = new MerchbarUrlModel();
			SourceType = DataSource.MerchbarUrl;
			LogoKey = LogoStyleKey.MerchbarUrlLogo;
			SideLogoKey = LeftSideBarLogoKey.MerchbarUrlSideLogo;
			Url = Urls.MerchbarUrl;
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
					MusConvAlbum album;
					try
					{
						album = await MerchbarUrlModel.GetAlbumAsync(item).ConfigureAwait(false);
					}
					catch
					{
						WrongURLs.Add(item);
						continue;
					}

					SelectedTab.MediaItems.Add(album);
				}

				await ShowWrongURLs();
				Initial_Setup();
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				await ShowError(Texts.EnterCorrectWebURLs);
				MainViewModel.NavigateTo(NavigationKeysChild.WebUrl);
			}
		}

		#endregion AuthMethods
	}
}