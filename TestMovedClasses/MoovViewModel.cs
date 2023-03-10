using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.Lib.Moov;
using System;
using System.Linq;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;
using System.Collections.Generic;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	class MoovViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public MoovViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Moov";
			Model = new MoovModel();
			SourceType = DataSource.Moov;
			LogoKey = LogoStyleKey.MoovLogo;
			SideLogoKey = LeftSideBarLogoKey.MoovSideLogo;
			Url = Urls.Moov;
			BaseUrl = Urls.Moov;

		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				SelectedTab.Loading = true;
				var client = new MoovParser();
				MainViewModel.NavigateTo(NavigationKeysChild.Content);
				SelectedTab.MediaItems.Clear();

				URLs = s.ToString().Split('\n').Where(x => !string.IsNullOrEmpty(x));
				var result = new List<MusConvPlayList>();
				foreach (var item in URLs)
				{
					try
					{
						var playlists = await (Model as MoovModel).GetPlaylists(item).ConfigureAwait(false);
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