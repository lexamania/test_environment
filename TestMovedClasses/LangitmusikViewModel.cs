using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.ViewModels.Base;
using System;
using static MusConv.MessageBoxManager.MessageBox;
using Client = MusConv.Lib.Langitmusik.Client;
using System.Linq;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Messages;
using System.Collections.Generic;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class LangitmusikViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public LangitmusikViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Langitmusik";
			SourceType = DataSource.Langitmusik;
			LogoKey = LogoStyleKey.LangitmusikLogo;
			SideLogoKey = LeftSideBarLogoKey.LangitmusikSideLogo;
			Url = "https://www.langitmusik.co.id";
			BaseUrl = "https://www.langitmusik.co.id";
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				var client = new Client();
				SelectedTab.Loading = true;
				MainViewModel.NavigateTo(NavigationKeysChild.Content);
				SelectedTab.MediaItems.Clear();

				URLs = s.ToString().Split('\n').Where(x => !string.IsNullOrEmpty(x));
				var result = new List<MusConvPlayList>();
				foreach (var item in URLs)
				{
					try
					{
						var playlist = await client.Get(item).ConfigureAwait(false);
						if (playlist is null)
						{
							WrongURLs.Add(item);
							continue;
						}
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