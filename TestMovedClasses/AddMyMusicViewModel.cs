using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;
using System;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Lib.AddMyMusic.Model;
using Client = MusConv.Lib.AddMyMusic.Client;
using System.Linq;
using System.Threading.Tasks;
using MusConv.ViewModels.Messages;
using System.Collections.Generic;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using MusConv.Abstractions.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class AddMyMusicViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public AddMyMusicViewModel(MainViewModelBase m) : base(m)
		{
			Title = "AllMyMusic";
			SourceType = DataSource.AddMyMusic;
			LogoKey = LogoStyleKey.AddMyMusicLogo;
			SideLogoKey = LeftSideBarLogoKey.AddMyMusicSideLogo;
			Url = "https://www.allmusic.com";
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				SelectedTab.Loading = true;
				var client = new Client();
				MainViewModel.NavigateTo(NavigationKeysChild.Content);
				SelectedTab.MediaItems.Clear();
				URLs = s.ToString().Split('\n').Where(x => !string.IsNullOrEmpty(x));

				foreach (var item in URLs)
				{
					var albums = new List<Album>();
					try
					{
						albums = await client.Get(item);
						if (albums.Count == 0)
						{
							WrongURLs.Add(item);
							continue;
						}
					}
					catch
					{
						WrongURLs.Add(item);
						continue;
					}

					var result = albums.AllIsNotNull()
						.Select(x => _mapper.PlaylistMapper.MapPlaylistWithTracks(x, x.AllTracks))
						.ToList();

					SelectedTab.Loading = false;
					
					SelectedTab.MediaItems.AddRange(result);
					
					albums.Clear();
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