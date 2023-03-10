using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.Lib.Melon;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using static MusConv.MessageBoxManager.MessageBox;
using System.Collections.Generic;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class MelonViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public MelonViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Melon";
			SourceType = DataSource.Melon;
			LogoKey = LogoStyleKey.MelonLogo;
			SideLogoKey = LeftSideBarLogoKey.MelonSideLogo;
			Url = Urls.Melon + "/chart/index.htm";
			BaseUrl = Urls.Melon;
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
						var playlist = await Client.GetPlayList(item).ConfigureAwait(false);
						var tracks = _mapper.TrackMapper.MapTracks(playlist.Tracks);
						result.Add(new MusConvPlayList("Melon", tracks));
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