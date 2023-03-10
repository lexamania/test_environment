using Avalonia.Threading;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MusConv.Lib.YouTubeApiRest;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using static MusConv.MessageBoxManager.MessageBox;
using System.Threading;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Abstractions.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class YouTubeOfficialViewModel : SectionViewModelBase
	{
		#region Fields

		public YouTubeService service;
		public OAuth2Client client;
		public static readonly string ClientID =
	"170749361877-faji4pcc28uqlirrsdk7ksc0i91jrpfe.apps.googleusercontent.com";
		public static readonly string ClientSecret = "f4DvnqNhsiaPZw58EStO0ltJ";
		public static readonly string RedirectURI = "urn:ietf:wg:oauth:2.0:oob";
		public static readonly string Scope = "https://www.googleapis.com/auth/youtube";

		#endregion Fields

		#region Constructors

		public YouTubeOfficialViewModel(MainViewModelBase m) : base(m)
		{
			Title = "YouTube";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.Youtube;
			LogoKey = LogoStyleKey.YoutubeLogo;
			SideLogoKey = LeftSideBarLogoKey.YoutubeSideLogo;
			Model = new YoutubeOfficialModel();
			MinimalLicenseForTransferFrom = Shared.Licensing.Enum.MusConvLicense.Ultimate;
			MinimalLicenseForTransferTo = Shared.Licensing.Enum.MusConvLicense.Ultimate;

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewOnYouTubeCommand(CommandTrack_Open)
				//new Command_TaskItem("Add to playlist...",Command_Track_AddToPlaylist),
				//	 new Command_TaskItem("Add track to playlist", Command_Track_AddToPlaylist, CommandTaskType.CommandBar,"addTrack")
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{  
				new ViewOnYouTubeCommand(CommandPlaylist_Open)
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewOnYouTubeCommand(CommandTrack_Open)
			};

			#endregion Commands

			#region TransferTasks

			var transfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				transfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				transfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase, 
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> Log_Out(bool arg)
		{
			RegState = RegistrationState.Unlogged;
			await client.LogOutAsync().ConfigureAwait(false);
			foreach (var t in Tabs)
			{
				await Dispatcher.UIThread.InvokeAsync(() => t.MediaItems.Clear());
			}
			Initial_Setup();
			return true;
		}

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(() => MainViewModel.NavigateTo(NavigationKeysChild.Content));
				if (service == null)
				{
					client = new OAuth2Client("MusConvY", ClientID, ClientSecret, Scope);
					if (await client.LoginAsync().ConfigureAwait(false))
					{
						RegState = RegistrationState.Logged;
						service = new YouTubeService(new BaseClientService.Initializer()
						{
							HttpClientInitializer = client.Cred,
							ApplicationName = "MusConv"
						});
						(Model as YoutubeOfficialModel).client = client;
						(Model as YoutubeOfficialModel).service = service;
						await Initial_Update_Playlists().ConfigureAwait(false);
					}
				}
				else
					await Initial_Update_Playlists().ConfigureAwait(false);

				return false;
			}
			catch (Exception ex)
			{
				Initial_Setup();
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		public async Task AuthorizeAsync()
		{
			if (service == null)
			{
				client = new OAuth2Client("MusConvY", ClientID, ClientSecret, Scope);
				if (await client.LoginAsync().ConfigureAwait(false))
				{
					RegState = RegistrationState.Logged;
					service = new YouTubeService(new BaseClientService.Initializer()
					{
						HttpClientInitializer = client.Cred,
						ApplicationName = "MusConv"
					});
				}
				(Model as YoutubeOfficialModel).client = client;
				(Model as YoutubeOfficialModel).service = service;
			}
		}

		#endregion AuthMethods

		#region InitialUpdate

		public override async Task<bool> Initial_Update_Playlists(bool forceUpdate = false)
		{
			var selectedTab = SelectedTab;

			try
			{
				var youtubeModel = Model as YoutubeModel;
				var loadingHandler = selectedTab.GetDetailedLoadingHandler();
				await InitialUpdateBuilder(() => youtubeModel.GetOfficialPlaylists(loadingHandler), selectedTab, forceUpdate);
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				if (e.Message.Contains("quota"))
				{
					await ShowMessage(Texts.YTQuotaLimit, Icon.Warning);
				}
				else
				{
					UnableToLoadPlaylists(e);
				}

				selectedTab.StopLoading();
				return false;
			}

			return true;
		}

		#endregion InitialUpdate

		#region OpeningMethods

		public Task CommandPlaylist_Open(object arg)
		{
			var arg1 = arg as MusConvPlayList;
			OpenUrlExtension.OpenUrl($"https://www.youtube.com/playlist?list={arg1.Id}");
			return Task.CompletedTask;
		}

		#endregion OpeningMethods

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			IsSending = true;
			if (service == null)
			{
				client = new OAuth2Client("MusConvY", ClientID, ClientSecret, Scope);
				var res = await client.LoginAsync().ConfigureAwait(false);
				if (res)
				{
					RegState = RegistrationState.Logged;
					service = new YouTubeService(new BaseClientService.Initializer()
					{
						HttpClientInitializer = client.Cred,
						ApplicationName = "MusConv"
					});
					WaitAuthentication.Set();
					IsSending = false;
					(Model as YoutubeOfficialModel).client = client;
					(Model as YoutubeOfficialModel).service = service;
					await Transfer_DoWork(items[0]);
				}
			}
			else
			{
				WaitAuthentication.Set();
				IsSending = false;
			}

			await Transfer_DoWork(items[0]);
		}

		public async Task<MusConvTrackSearchResult> Transfer_Search(int index, MusConvTrack track,
			IProgress<ReportCount> arg3, CancellationToken token)
		{
			try
			{
				var search =
					$"\"{track.Title}\" by \"{track.Artist}\"";
				arg3.Report(new ReportCount(index, $"Searching: {search} ", ReportType.Searching));
				var tMusConvSearchResult = await DefaultSearchAsync(track, token).ConfigureAwait(false);
				return tMusConvSearchResult;
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("quota"))
				{
					await ShowMessage(Texts.YTQuotaLimit, Icon.Warning).ConfigureAwait(false);
				}
				MusConvLogger.LogFiles(ex);
				Initial_Setup();
				return new MusConvTrackSearchResult(track);
			}
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			try
			{
				int i = 0;
				foreach (var resultKey in result.Keys)
				{
					var list = result[resultKey].Select(y => y.ResultItems?.FirstOrDefault()).AllIsNotNull();

					var playlist = new Playlist();
					playlist.Status = new PlaylistStatus { PrivacyStatus = "private" };;
					playlist.Snippet = new PlaylistSnippet { Title = resultKey.Title, Description = VariableTexts.GetItemDescriptionFromSourceItem(resultKey) };

					var request = service.Playlists.Insert(playlist, "snippet,status");
					var response = await request.ExecuteAsync().ConfigureAwait(false);

					foreach (var item in list)
					{
						PlaylistItem playlistItem = new PlaylistItem();
						var snippet2 = new PlaylistItemSnippet 
						{ 
							PlaylistId = response.Id,
							ResourceId = new ResourceId 
							{ 
								Kind = item.GetAdditionalProperty(PropertyType.Kind), 
								VideoId = item.GetAdditionalProperty(PropertyType.VideoId) 
							}
						};
						playlistItem.Snippet = snippet2;

						var request2 = service.PlaylistItems.Insert(playlistItem, "snippet");
						await request2.ExecuteAsync().ConfigureAwait(false);
					}
					i++;
				}
				progressReport.Report(GetPlaylistsReportCount(result));
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("quota"))
				{
					await ShowMessage(Texts.YTQuotaLimit, Icon.Warning).ConfigureAwait(false);
				}
				MusConvLogger.LogFiles(ex);
				Initial_Setup();
			}
		}

		#endregion TransferMethods
	}
}