using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.MessageBoxManager.Texts;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Attributes;
using System.Collections.Generic;
using MusConv.Lib.VirtualDJUrl;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class VirtualDJUrlViewModel : WebUrlViewModelBase
	{
		#region Constructors

		public VirtualDJUrlViewModel(MainViewModelBase main) : base(main)
		{
			Title = "Virtual DJ Url";
			SourceType = DataSource.VirtualDJUrl;
			LogoKey = LogoStyleKey.VirtualDJUrlLogo;
			SideLogoKey = LeftSideBarLogoKey.VirtualDJUrlSideLogo;
			Url = Urls.VirtualDJUrl;
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			try
			{
				SelectedTab.Loading = true;
				var client = new VirtualDJParser();
				MainViewModel.NavigateTo(NavigationKeysChild.Content);
				SelectedTab.MediaItems.Clear();
				
				URLs = s?.ToString()?.Split('\n').Where(x => !string.IsNullOrEmpty(x));
				var result = new List<MusConvPlayList>();
				foreach (var item in URLs)
				{
					try
					{
						if(!await client.SetUrlAsync(item).ConfigureAwait(false))
						{
							WrongURLs.Add(item);
							continue;
						}

						var playlist = client.GetPlaylist();
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

		#region TransferMethods

		public override Task Transfer_SaveInTo(object[] items)
		{
		   return ShowTransferToHelpOnDemandAsync(MessageBoxText.VirtualDJHelp);
		}

		#endregion TransferMethods
	}
}