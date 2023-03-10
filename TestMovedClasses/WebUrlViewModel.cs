using Avalonia.Threading;
using MusConv.MessageBoxManager.Enums;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.WebViewViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class WebUrlViewModel : SectionViewModelBase
	{
		#region Constructors

		public WebUrlViewModel(MainViewModelBase main) : base(main)
		{
			Title = "Web URL";
			SourceType = DataSource.ImportByLink;
			LogoKey = LogoStyleKey.ImportByLinkLogo;
			SideLogoKey = LeftSideBarLogoKey.ImportByLinkSideLogo;
			RegState = RegistrationState.Needless;
			Model = new ImportByLinkModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnSourceServiceCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewAlbumCommand(CommandTrack_OpenAlbum),
			};

			#endregion Commands

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, null, null, main)
			};

			var playlsitsTab = new PlaylistTabViewModelBase(main, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks);

			Tabs.Add(playlsitsTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			MainViewModel.NeedLogin = this;
			if (Tabs.FirstOrDefault().MediaItems.Any() && playlists.Count != 0)
			{
				SelectedTab.MediaItems.Clear();
				SelectedTab.MediaItems.AddRange(playlists);
			}
			else
			{
				await Dispatcher.UIThread.InvokeAsync(() => MainViewModel.NavigateTo(NavigationKeysChild.WebUrl));
			}
			return true;
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			MainViewModel.NeedLogin = this;
			SelectedTab.Loading = true;

			var links = new List<Uri>();
			var failedUrls = new Dictionary<Uri, Exception>();
			var failedInputs = new List<string>();
			bool youTubeLogInError = false;
			WebUrlViewModelBase.ActiveUrl = s?.ToString() ?? "";
			NavigateToContent();
			var text = s as string;
			if (!string.IsNullOrEmpty(text))
			{
				var lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in lines)
				{
					if (IsValidUrl(line))
					{
						links.Add(new Uri(line));
					}
					else
					{
						failedInputs.Add(line);
					}
				}

				//bad way but so far the only one
				var youtubeClient = (MainViewModel.SideItems.Where(x => x.LogoKey == LogoStyleKey.YoutubeLogo).FirstOrDefault().Model as YoutubeModel).Client;
				if (youtubeClient == null)
				{
					await (MainViewModel.SideItems.Where(x => x.Title == "YouTube").FirstOrDefault() as YoutubeViewModel)
						.IsServiceSelectedAsyncWebUrl();

					youtubeClient = (MainViewModel.SideItems.Where(x => x.LogoKey == LogoStyleKey.YoutubeLogo)
						.FirstOrDefault().Model as YoutubeModel)
						.Client;
				}

				var importLinkModel = (ImportByLinkModel)Model;

				(playlists, failedUrls, youTubeLogInError) = await importLinkModel.GetAllPlaylist(links);

				if (youTubeLogInError)
				{
					var result = await ShowLogInWarning(Texts.LogInWarningMessage);

					if (result == ButtonResult.LogIn)
					{
						MainViewModel.SelectedItem = MainViewModel.SideItems.FirstOrDefault(x => x.Title == "YouTube");
					}
					else
					{
						youTubeLogInError = false;
					}
				}
				else if (failedUrls.Count > 0)
				{
					var warningMessage = new StringBuilder(
						"Please ensure your playlists are public."
						+ Environment.NewLine + Environment.NewLine +
						"Couldn't process web url:");

					if (failedUrls.Count > 1)
						warningMessage.Insert(warningMessage.Length - 1, "s");

					foreach (var item in failedUrls)
					{
						warningMessage.Append(Environment.NewLine + item.Key);

						if (item.Value is not null)
						{
							MusConvLogger.LogFiles(item.Value);
						}
					}

					warningMessage.Append(
						Environment.NewLine + Environment.NewLine +
						Texts.AttachWebUrlsForInvestigation);

					if (failedUrls.Count <= 6)
						await ShowWarning(warningMessage.ToString());
					else
						await ShowWarningWithScrollBar(warningMessage.ToString());
				}
				else if (failedInputs.Count != 0)
				{
					var message = new StringBuilder("Could not proccess the following inputs:" + Environment.NewLine);

					foreach (var item in failedInputs)
						message.Append(item + Environment.NewLine);

					message.Append("Please input valid urls");

					await ShowError(message.ToString());
				}
			}

			if (SelectedTab?.MediaItems == null || SelectedTab.MediaItems.Count != 0) return;

			if (youTubeLogInError) return;

			if (playlists.Any())
			{
				SelectedTab.MediaItems?.Clear();
				SelectedTab.MediaItems.AddRange(playlists);
				Initial_Setup();

				return;
			}

			await IsServiceSelectedAsync();
		}

		#endregion AuthMethods

		#region InitialUpdate

		public async void Initial_Update_Playlists(bool forceUpdate = false)
		{
			MainViewModel.NeedLogin = this;
			playlists = new List<MusConvPlayList>();
			await Dispatcher.UIThread.InvokeAsync(() => MainViewModel.NavigateTo(NavigationKeysChild.WebUrl));
		}

		#endregion InitialUpdate

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] arg)
		{
			try
			{
				var playLists = arg[0] as List<MusConvPlayList>;
				var tracks = playLists.SelectMany(x => x.AllTracks).Cast<MusConvModelBase>().ToList();
				
				var savePath = await GetSavePath(playLists.First().Title, new() { "txt" });

				if (string.IsNullOrEmpty(savePath))
					return;
			
				await Model.ExportAsUrl(tracks, savePath).ConfigureAwait(false);
				await ShowMessage("Items exported successfully!");
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
			}
		}

		#endregion TransferMethods

		#region InnerMethods

		private static bool IsValidUrl(string url)
		{
			if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) return false;
			if (!Uri.TryCreate(url, UriKind.Absolute, out var tmp)) return false;
			return tmp.Scheme == Uri.UriSchemeHttp || tmp.Scheme == Uri.UriSchemeHttps;
		}

		#endregion InnerMethods
	}
}