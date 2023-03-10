using Avalonia.Threading;
using MusConv.Lib.KuGou;
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
	public class KuGouViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public KuGouViewModel(MainViewModelBase main) : base(main)
		{
			Title = "kugou.com";
			SourceType = DataSource.KuGou;
			LogoKey = LogoStyleKey.KuGouLogo;
			SideLogoKey = LeftSideBarLogoKey.KuGouSideLogo;
			Url = Urls.KuGou;
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				SelectedTab.Loading = true;
				var client = new KuGouParser();
				MainViewModel.NavigateTo(NavigationKeysChild.Content);
				SelectedTab.MediaItems.Clear();

				URLs = s.ToString().Split('\n').Where(x => !string.IsNullOrEmpty(x));
				var result = new List<MusConvPlayList>();
				foreach (var item in URLs)
				{
					try
					{
						var playlist = await client.SetUrlAsync(item).ConfigureAwait(false);
						var tracks = _mapper.TrackMapper.MapTracks(playlist.AllTracks);
						result.Add(new MusConvPlayList(playlist.Name, tracks));
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