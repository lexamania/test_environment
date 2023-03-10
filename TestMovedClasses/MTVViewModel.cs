using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.Lib.MTV;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using static MusConv.MessageBoxManager.MessageBox;
using System.Collections.Generic;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class MTVViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public MTVViewModel(MainViewModelBase main) : base(main)
		{
			Title = "MTV";
			LogoKey = LogoStyleKey.MTVLogo;
			SideLogoKey = LeftSideBarLogoKey.MTVSideLogo;
			SourceType = DataSource.MTV;
			Url = Urls.MTV + "/music/playlists";
			BaseUrl = Urls.MTV;

		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			SelectedTab.Loading = true;
			try
			{
				var client = new Client();
				NavigateToContent();
				SelectedTab.MediaItems.Clear();

				URLs = s.ToString().Split('\n').Where(x => !string.IsNullOrEmpty(x));
				var result = new List<MusConvPlayList>();
				foreach (var item in URLs)
				{
					try
					{
						var playlist = await client.GetSongAsync(item).ConfigureAwait(false);
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