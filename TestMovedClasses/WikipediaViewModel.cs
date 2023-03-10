using Avalonia.Threading;
using MusConv.Lib.Wikipedia;
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
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class WikipediaViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public WikipediaViewModel(MainViewModelBase main) : base(main)
		{
			Title = "Wikipedia";
			SourceType = DataSource.Wikipedia;
			LogoKey = LogoStyleKey.WikipediaLogo;
            SideLogoKey = LeftSideBarLogoKey.WikipediaSideLogo;
			Url = Urls.Wikipedia + "/wiki/List_of_2021_albums_(January–June)";
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				SelectedTab.Loading = true;
				var client = new ParseWikipediaPlaylist();
				MainViewModel.NavigateTo(NavigationKeysChild.Content);
				SelectedTab.MediaItems.Clear();

				URLs = s.ToString().Split('\n').Where(x => !string.IsNullOrEmpty(x));
				var result = new List<MusConvPlayList>();
				foreach (var item in URLs)
				{
					try
					{
						if(!await client.SetUrlAsync(item).ConfigureAwait(false))
						{
							WrongURLs.Add(item);
							continue;
						}

						var playlist = client.GetPlaylist();
						result.Add(_mapper.PlaylistMapper.MapPlaylistWithTracks(playlist, playlist.AllTracks));
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
