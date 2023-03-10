using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    [WebUrlParser]
    public class HmvStoreViewModel : WebUrlViewModelBase
	{
		#region Fields

		private HmvStoreModel HmvStoreModel => Model as HmvStoreModel;

		#endregion Fields

		#region Constructors

		public HmvStoreViewModel(MainViewModelBase main) : base(main)
		{
			Title = "HmvStore";
			Model = new HmvStoreModel();
			SourceType = DataSource.HmvStore;
			LogoKey = LogoStyleKey.HmvLogo;
			SideLogoKey = LeftSideBarLogoKey.HmvSideLogo;
			Url = Urls.HmvStore;
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
						album = await HmvStoreModel.GetAlbumAsync(item).ConfigureAwait(false);
					}
					catch
					{
						MusConvLogger.LogInfo("Wrong url detected");
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