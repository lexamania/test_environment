using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Lib.Genie;
using MusConv.ViewModels.ViewModels.Base;
using System.Linq;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Messages;
using static MusConv.MessageBoxManager.MessageBox;
using System.Collections.Generic;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class GenieViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public GenieViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Genie";
			SourceType = DataSource.Genie;
			LogoKey = LogoStyleKey.GenieLogo;
			SideLogoKey = LeftSideBarLogoKey.GenieSideLogo;
			Url = Urls.Genie;
			BaseUrl = Urls.Genie;
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
						var tracks = _mapper.TrackMapper.MapTracks(playlist.Tracks);
						result.Add(new MusConvPlayList("Genie", tracks));
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