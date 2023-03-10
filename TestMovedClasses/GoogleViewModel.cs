using Avalonia.Threading;
using GooglePlayMusicAPI;
using GooglePlayMusicAPI.Models.GooglePlayMusicModels;
using MusConv.MessageBoxManager.Texts;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Abstractions.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class GoogleViewModel:SectionViewModelBase
	{
		#region Fields

		private GooglePlayMusicClient _googleApi;
		private string ClientId = "228293309116.apps.googleusercontent.com";
		private string ClientSecret = "GL1YV0XMp0RlL7ylCV3ilFz-";
		private string Scope = "https://www.googleapis.com/auth/skyjam";

		#endregion Fields

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			MainViewModel.NeedLogin = this;
			await Dispatcher.UIThread.InvokeAsync(() => MainViewModel.NavigateTo(NavigationKeysChild.Content));
			if (_googleApi == null) _googleApi = new GooglePlayMusicClient("MusConv", ClientId, ClientSecret, Scope);
			if (await _googleApi.LoginAsync().ConfigureAwait(false))
			{
				Model = new Google2Model() { _googleApi = this._googleApi };
				RegState = RegistrationState.Logged;
				await InitialUpdateForCurrentTab().ConfigureAwait(false);
			}

			return false;
		}

		public override async Task<bool> Log_Out(bool arg)
		{
			RegState = RegistrationState.Unlogged;
			await _googleApi.LogOutAsync().ConfigureAwait(false);
			foreach (var t in Tabs)
			{
				await Dispatcher.UIThread.InvokeAsync(() => t.MediaItems.Clear());
			}
			SelectedTab = Tabs.FirstOrDefault();
			await IsServiceSelectedAsync().ConfigureAwait(false);
			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

		private async Task<bool> Initial_Update_ThumbsUp(bool arg)
		{
			var thumbsupTracks = await _googleApi.GetThumbsUpAsync();
			var result = _mapper.TrackMapper.MapTracks(thumbsupTracks);
			await Initial_Update_Custom(result, arg).ConfigureAwait(false);
			return true;
		}

		#endregion InitialUpdate

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
		return ShowHelp(MessageBoxText.GMusicHelp);
		}

		#endregion Commands

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			IsSending = true;
			MainViewModel.NeedLogin = this;
			if (_googleApi == null) _googleApi = new GooglePlayMusicClient("MusConv", ClientId, ClientSecret, Scope);
			if (await _googleApi.LoginAsync().ConfigureAwait(false))
			{
				Model = new Google2Model() { _googleApi = this._googleApi };
				RegState = RegistrationState.Logged;
				IsSending = false;
			}

			WaitAuthentication.Set();
			await Transfer_DoWork(items[0]).ConfigureAwait(false);
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			int i = 0;
			foreach (var resultKey in result.Keys)
			{
				var tracks = result[resultKey].Select(y => y.ResultItems?.FirstOrDefault()).AllIsNotNull();

				//Тут разбиваем список на подскписки по 999 треков т.к Гугл не позволяет добавить
				//в плейлист больше 1000 треков
				var splitedTracks = tracks.ToList().SplitList(999).ToList(); 

				foreach (var trackList in splitedTracks)
				{
					var resp = await _googleApi.CreatePlaylistAsync(splitedTracks.Count == 1 
							? resultKey.Title 
							: $"{resultKey.Title} №{splitedTracks.IndexOf(trackList) + 1}", 
						VariableTexts.GetItemDescriptionFromSourceItem(resultKey))
					.ConfigureAwait(false);

					foreach (var tracksPart in trackList.Distinct().ToList().SplitList())
					{
						var first = tracksPart.FirstOrDefault();
						token.ThrowIfCancellationRequested();
						progressReport.Report(new ReportCount(index,
							$"Adding \"{first?.Title}\" by \"{first?.Artist}\" to playlist",
							ReportType.Sending));
						try
						{
							var songs = tracksPart.Select(x => new Track()
							{
								Id = x.Id,
								StoreID = x.GetAdditionalProperty(PropertyType.StoreId)
							}).ToList();

							await _googleApi.AddToPlaylistAsync(resp.MutateResponses.FirstOrDefault().ID, songs,
									VariableTexts.GetItemDescriptionFromSourceItem(resultKey)).ConfigureAwait(false);
						}
						catch (Exception ex)
						{
							MusConvLogger.LogFiles(ex);
						}
					}
				}

				i++;
			}

			progressReport.Report(GetPlaylistsReportCount(result));
		}

		private async Task TransferTrack_Send(Dictionary<string, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			var content = $"Adding tracks, please wait";
			progressReport.Report(new ReportCount(0, content,
				ReportType.Sending));
			int tracksCount = 0;
			foreach (var resultKey in result.Keys)
			{
				var tracks = result[resultKey].Select(y => y.ResultItems?.FirstOrDefault()).AllIsNotNull().ToList();
				if (tracks != null) tracksCount = tracks.Count;

				foreach (var track in tracks)
				{
					token.ThrowIfCancellationRequested();
					if (track == null) continue;

					try
					{
						var song = new Track()
						{
							Id = track.Id,
							NID = track.GetAdditionalProperty(PropertyType.NId),
							TrackAvailableForSubscription = Convert.ToBoolean(track.GetAdditionalProperty(PropertyType.AvailableForSubscription)),
						};
						await _googleApi.AddToStoreAsync(song);
					}
					catch (Exception ex)
					{
						MusConvLogger.LogFiles(ex);
					}
					progressReport.Report(new ReportCount(1, $"Adding track \"{track.Title}\" to library",
						ReportType.Sending));
				}
			}

			progressReport.Report(new ReportCount(-1,
				$"{tracksCount} track{(tracksCount > 1 ? "s " : " ")} successfully transferred",
				ReportType.Sending));
		}

		#endregion TransferMethods

		#region InnerMethods

		public GoogleViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Google Music";
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.GoogleMusic;
			LogoKey = LogoStyleKey.PlayMusicLogo;
			Model = new Google2Model();
			CurrentVMType = VmType.Service;

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ViewOnGoogleMusicCommand(CommandTrack_Open),
				new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
				new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu), //Оставлю пока что как есть.с двойным повторением,так архитектура построена,что Default отвечает за отображение комманды в кнопке на плейлисте,а MainMenu за отображение в самой табе
				new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewOnGoogleMusicCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
			{
				new ViewOnGoogleMusicCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new ViewArtistCommand(CommandTrack_OpenArtist),
				new ViewOnGoogleMusicCommand(CommandTrack_Open),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnGoogleMusicCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands

			#region TransferTasks

			var transfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_SearchWithAlbums, Transfer_Send, m),
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m),
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send, m),
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				transfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				transfer, commandAlbumsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				transfer, commandArtistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				transfer, commandTracksTab, 
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			var thumbsUpTab = new TrackTabViewModelBase(m, "Thumbs Up", LwTabIconKey.TrackIcon, 
				transfer, commandTracksTab, 
				new Initial_TaskItem("Reload", Initial_Update_ThumbsUp), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
			Tabs.Add(thumbsUpTab);
		}

		#endregion InnerMethods
	}
}