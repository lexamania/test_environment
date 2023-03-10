using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.Lib.MusicBrainz;
using System;
using System.Linq;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;
using System.Collections.Generic;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Models;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	class MusicBrainzViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public MusicBrainzViewModel(MainViewModelBase main) : base(main)
		{
			Title = "MusicBrainz";
			Model = new MusicBrainzModel();
			LogoKey = LogoStyleKey.MusicBrainzLogo;
			SideLogoKey = LeftSideBarLogoKey.MusicBrainzSideLogo;
			SourceType = DataSource.MusicBrainz;
			Url = Urls.MusicBrainz;
			BaseUrl = Urls.MusicBrainz;
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				SelectedTab.Loading = true;
				var client = new MusicBrainzClient();
				MainViewModel.NavigateTo(NavigationKeysChild.Content);
				SelectedTab.MediaItems.Clear();

				URLs = s.ToString().Split('\n').Where(x => !string.IsNullOrEmpty(x));
				var result = new List<MusConvPlayList>();
				foreach (var item in URLs)
				{
					try
					{
						var playlists = await (Model as MusicBrainzModel).GetPlaylists(item).ConfigureAwait(false);
						if (playlists.Count == 0) 
						{
							WrongURLs.Add(item);
							continue;
						}
						result.AddRange(playlists);
					}
					catch
					{
						WrongURLs.Add(item);
						continue;
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