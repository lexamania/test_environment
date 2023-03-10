using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Lib.Groovershark;
using MusConv.Sentry;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.Messages;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Navigation.Keys.NavigationKeys;
using System.Collections.Generic;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class GroovesharkViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public GroovesharkViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Grooveshark";
			SourceType = DataSource.Grooveshark;
			LogoKey = LogoStyleKey.GroovesharkLogo;
			SideLogoKey = LeftSideBarLogoKey.GroovesharkSideLogo;
			Url = "https://groovesharks.org/tag/all";
			BaseUrl = "https://groovesharks.org";
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
						result.Add(new MusConvPlayList(playlist.Title, tracks));
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