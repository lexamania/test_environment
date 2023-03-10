using Avalonia.Threading;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Lib.RedditUrl;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	[WebUrlParser]
	public class RedditUrlViewModel : WebUrlViewModelBase
	{
		#region Fields

		private RedditUrlModel RedditUrlModel => Model as RedditUrlModel;

		#endregion Fields

		#region Constructors

		public RedditUrlViewModel(MainViewModelBase m) : base(m)
		{
			Model = new RedditUrlModel();
			RedditUrlModel.OnStateChanged += ChangeStatusMessage;
			Title = "RedditUrl";
			SourceType = DataSource.RedditUrl;
			LogoKey = LogoStyleKey.RedditUrlLogo;
			SideLogoKey = LeftSideBarLogoKey.RedditUrlSideLogo;
			Url = "https://www.reddit.com/";

			#region Commands

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeMusicCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands

			#region Tabs

			var albumsTab = new AlbumTabViewModelBase(m, AppTabs.Albums, LwTabIconKey.AlbumIcon,
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistsTab = new ArtistTabViewModelBase(m, AppTabs.Artists, LwTabIconKey.ArtistIcon,
				EmptyTransfersBase, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
		}

		#endregion Constructors

		#region DataProperties

		private List<string> RedditLinks { get; set; } = new();

		#endregion DataProperties

		#region AccountsProperties

		private List<string> RedditLinks { get; set; } = new();

		#endregion AccountsProperties

		#region MainProperties

		private List<string> RedditLinks { get; set; } = new();

		#endregion MainProperties

		#region ItemsProperties

		private List<string> RedditLinks { get; set; } = new();

		#endregion ItemsProperties

		#region AuthMethods

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			URLs = s.ToString().Split(Environment.NewLine).Where(x => !string.IsNullOrEmpty(x));
			WrongURLs = await DetectWrongUrls();

			foreach (var tab in Tabs)
				tab.MediaItems.Clear();

			RedditLinks.Clear();
			RedditLinks.AddRange(URLs.Where(x => !WrongURLs.Contains(x)));
			await ShowWrongURLs();


			if (RedditLinks.Count != 0)
			{
				MainViewModel.NavigateTo(NavigationKeysChild.Content);
				await InitialUpdateForCurrentTab(true);
			}
		}

		#endregion AuthMethods

		#region InitialUpdate

		public override void Initial_Update_Playlists(bool forceUpdate = false)
		{
			_ = UpdateTab<MusConvPlayList, PlaylistTabViewModelBase>(RedditUrlModel.GetPlaylists);
		}

		public void Initial_Update_Album(bool forceUpdate = false)
		{
			_ = UpdateTab<MusConvAlbum, AlbumTabViewModelBase>(RedditUrlModel.GetAlbums);
		}

		public void Initial_Update_Artists(bool forceUpdate = false)
		{
			_ = UpdateTab<MusConvArtist, ArtistTabViewModelBase>(RedditUrlModel.GetArtists);
		}

		#endregion InitialUpdate

		#region InnerMethods

		private async Task UpdateTab<Item, Tab>(Func<string, Task<List<Item>>> method) where Item : MusConvItemBase where Tab : TabViewModelBase
		{
			try
			{
				SelectedTab.Loading = true;
				SelectedTab.MediaItems.Clear();

				var result = new List<Item>();
				foreach (var item in RedditLinks)
				{
					try
					{
						result.AddRange(await method(item));
					}
					catch (Exception ex)
					{
						MusConvLogger.LogFiles(ex);
						continue;
					}

					if (SelectedTab is not Tab) return;
				}

				await Dispatcher.UIThread.InvokeAsync(() =>
				{
					SelectedTab.MediaItems.Clear();
					SelectedTab.MediaItems.AddRange(result);
				});

				Initial_Setup();
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				await ShowError(Texts.EnterCorrectWebURLs);
				MainViewModel.NavigateTo(NavigationKeysChild.WebUrl);
			}
		}

		private void ChangeStatusMessage(string str)
		{
			SelectedTab.LoadingText = str;
		}

		private async Task<List<string>> DetectWrongUrls()
		{
			var result = new List<string>();

			foreach(var link in URLs)
			{
				try
				{
					await RedditParser.ParseReddit(link).ConfigureAwait(false);
				}
				catch(Exception ex)
				{
					MusConvLogger.LogInfo("Detected wrong url");
					result.Add(link);
				}
			}

			return result;
		}

		#endregion InnerMethods
	}
}