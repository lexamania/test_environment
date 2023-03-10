using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using static MusConv.MessageBoxManager.MessageBox;
using System.Collections.Generic;
using MusConv.Sentry.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Models.MusicService;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	class TuneFindUrlViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public TuneFindUrlViewModel(MainViewModelBase m) : base(m)
		{
			Title = "TuneFind Url";
			SourceType = DataSource.TuneFindUrl;
			LogoKey = LogoStyleKey.TuneFindUrlLogo;
			SideLogoKey = LeftSideBarLogoKey.TuneFindUrlSideLogo;
			Url = Urls.TuneFind;
			BaseUrl = Urls.TuneFind;
			Model = new TuneFindUrlModel();
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			SelectedTab.Loading = true;
			try
			{
				MainViewModel.NavigateTo(NavigationKeysChild.Content);
				SelectedTab.MediaItems.Clear();

				URLs = s.ToString().Split('\n').Where(x => !string.IsNullOrEmpty(x));
				var result = new List<MusConvPlayList>();
				foreach (var item in URLs)
				{
					try
					{
						var playlist = await Model.GetPlaylistByIdWithTracks(item);
						result.Add(playlist);
					}
					catch(Exception ex)
					{
						MusConvLogger.LogFiles(SentryEventBuilder.Build(ex).WithAdditionalInfo("Url", item));
						
						WrongURLs.Add(item);
					}
				}

				SelectedTab.MediaItems.AddRange(result);
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