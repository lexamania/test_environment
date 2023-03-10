using Avalonia.Threading;
using MusConv.Lib.Musi;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class MusiViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public MusiViewModel(MainViewModelBase main) : base(main)
		{
			Title = "Musi";
			SourceType = DataSource.Musi;
			LogoKey = LogoStyleKey.MusiLogo;
			SideLogoKey = LeftSideBarLogoKey.MusiSideLogo;
			Url = Urls.Musi;
			IsTransferAvailable = true;
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				SelectedTab.Loading = true;
				var client = new MusiParser();
				MainViewModel.NavigateTo(NavigationKeysChild.Content);
				SelectedTab.MediaItems.Clear();

				URLs = s.ToString().Split(Environment.NewLine).Where(x => !string.IsNullOrEmpty(x));
				var result = new List<MusConvPlayList>();
				foreach (var item in URLs)
				{
					try
					{
						var playlist = client.GetPlaylist(item);
						var tracks = _mapper.TrackMapper.MapTracks(playlist.Data);
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