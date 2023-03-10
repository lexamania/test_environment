using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Lib.GlobalUnderground;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.ViewModels.Base;
using static MusConv.MessageBoxManager.MessageBox;
using System.Collections.Generic;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class GlobalUndergroundViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public GlobalUndergroundViewModel(MainViewModelBase main) : base(main)
		{
			Title = "Global Underground";
			SourceType = DataSource.GlobalUnderground;
			LogoKey = LogoStyleKey.GlobalUndergroundLogo;
			SideLogoKey = LeftSideBarLogoKey.GlobalUndergroundSideLogo;
			Url = Urls.GlobalUnderground + "/music";
			BaseUrl = Urls.GlobalUnderground;
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
				var result = new List<MusConvPlayList>();
				foreach (var item in URLs)
				{
					try
					{
						var playlist = await Client.GetPlayList(item).ConfigureAwait(false);
						result.Add(_mapper.PlaylistMapper.MapPlaylistWithTracks(playlist, playlist.Songs));
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