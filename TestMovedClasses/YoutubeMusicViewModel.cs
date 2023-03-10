using MusConv.Abstractions.Extensions;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Collections.SearchResult;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Interfaces;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Models.MusicService.Base;
using MusConv.ViewModels.Settings;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusConv.ViewModels.ViewModels.WebViewViewModels
{
    // displays data from https://music.youtube.com/library
    public class YoutubeMusicViewModel : YouTubeViewModelBase, IHighlightableCommands
	{
		#region Fields

        private readonly StatsViewModelBase _statsTab;
        private readonly StatsCategoryTab artistsStatsTab;
        private readonly StatsCategoryTab tracksStatsTab;
        private readonly StatsCategoryTab albumsStatsTab;
        private readonly StatsCategoryTab genresStatsTab;
        private readonly StatsContentTab emptyContentTab;
        public Dictionary<TabViewModelBase, Command_TaskItem[]> HighlightableCommands { get; }

		#endregion Fields

		#region Constructors

        public YoutubeMusicViewModel(MainViewModelBase m) : base(m)
        {
            Title = "YouTube Music";
            SourceType = DataSource.YoutubeMusic;
            LogoKey = LogoStyleKey.YoutubeMusicLogo;
            SmallLogoKey = LogoStyleKey.YoutubeMusicLogoSmall;
            SideLogoKey = LeftSideBarLogoKey.YoutubeMusicSideLogo;
            BaseUrl = "https://music.youtube.com/";
            ArtistDirectUrl = "https://music.youtube.com/channel/";
            AlbumDirectUrl = "https://music.youtube.com/playlist?list=";
            SearchUrl = "https://music.youtube.com/search?q=";
            Model = CreateYouTubeModel();

            NavigateHelpCommand = ReactiveCommand.Create(() =>
                m.NavigateTo(NavigationKeysChild.YoutubeMusicHelp));

            #region Commands

            var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
            {
                new ViewOnYouTubeMusicCommand(CommandTrack_Open),
                new ViewAlbumCommand(CommandTrack_OpenAlbum),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
                new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
                new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
                new RecommendPlaylistCommand(Command_RecommendedPlaylists_Open, CommandTaskType.DropDownMenu),
            };

            var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
            {
                new ViewOnYouTubeMusicCommand(CommandTrack_Open),
                new ViewAlbumCommand(CommandTrack_OpenAlbum),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
                new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
                new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
                new AddTracksCommand(Command_AddTrack, CommandTaskType.CommandBar),
                new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
                new RecommendPlaylistCommand(Command_RecommendedPlaylists_Open, CommandTaskType.DropDownMenu),
            };

            var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
            {
                new ViewOnYouTubeMusicCommand(CommandTrack_Open),
                new ViewAlbumCommand(CommandTrack_OpenAlbum),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
                new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
                new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
                new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
                new RecommendPlaylistCommand(Command_RecommendedPlaylists_Open, CommandTaskType.DropDownMenu),
                new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
                //new HelpCommand(Command_Help, CommandTaskType.CommandBar)
            };

            var commandUploadsTab = new List<Command_TaskItem>(commandTracksTab)
			{
				new UploadCommand(Command_Upload, CommandTaskType.CommandBar)
			};

            var commandBarCreateSmartPlaylistCommand = new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.CommandBar);
            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
            {
                new ViewOnYouTubeMusicCommand(CommandPlaylist_Open),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
                new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.CommandBar),
                new DeleteDuplicatesCommand(Command_DeleteDuplicates, CommandTaskType.DropDownMenu),
                new EmptyCommand(Command_Empty, CommandTaskType.CommandBar),
                new EmptyCommand(Command_Empty, CommandTaskType.DropDownMenu),
                new CloneCommand(Command_Clone, CommandTaskType.CommandBar),
                new CloneCommand(Command_Clone, CommandTaskType.DropDownMenu),
                new SplitCommand(Command_Split, CommandTaskType.CommandBar),
                new SplitCommand(Command_Split, CommandTaskType.DropDownMenu),
                new EditCommand(Command_Edit, CommandTaskType.CommandBar),
                new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
                new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
                new CreatePlaylistCommand(Command_CreatePlaylist, CommandTaskType.CommandBar),
                new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.CommandBar),
                new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
                new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
                new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.DropDownMenu),
                new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.DropDownMenu),
                new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.CommandBar),
                new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
                new SortCommand(Command_Sort, CommandTaskType.CommandBar),
                commandBarCreateSmartPlaylistCommand,
                new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.DropDownMenu),
            };

            var commandSavedTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
            {
                new ViewOnYouTubeMusicCommand(CommandPlaylist_Open),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
                new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
                new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
            };
            
            var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
            {
                new ViewOnYouTubeMusicCommand(CommandArtist_Open),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
            };

            var commandSubscriptionsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
            {
                new ViewOnYouTubeMusicCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
                new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
            };

            var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
            {
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
                new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
                new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
                new ExportUrlsCommand(Command_ExportUrls, CommandTaskType.CommandBar),
            };

            #endregion Commands

            #region Transfer

            var albumTransfer = new List<TaskBase_TaskItem>
            {
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m, true)
			};
            var trackTransfer = new List<TaskBase_TaskItem>
            {
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m, true)
			};
            var playlistTransfer = new List<TaskBase_TaskItem>
            {
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};
            var artistsTransfer = new List<TaskBase_TaskItem>
            {
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};

            #endregion Transfer

            #region Tabs

            #region Stats

            emptyContentTab =  new StatsContentTab(MainViewModel, "Recap", 
				new Initial_TaskItem("Reload", () => { }), 
				EmptyTransfersBase, EmptyCommandsBase, 
				LwTabStyleKey.ItemStyleBlank);

            artistsStatsTab = new StatsCategoryTab("Top Artists", StatsTabIconsStyleKey.YouTubeTopArtists,
                Array.Empty<StatsContentTab>(), new Initial_TaskItem("Initialize", InitializeArtistsTab));

            tracksStatsTab = new StatsCategoryTab("Top Tracks", StatsTabIconsStyleKey.YouTubeTopTracks,
                Array.Empty<StatsContentTab>(),  new Initial_TaskItem("Initialize", InitializeTracksTab));

            albumsStatsTab = new StatsCategoryTab("Top Albums", StatsTabIconsStyleKey.AppleTopAlbums,
                Array.Empty<StatsContentTab>(), new Initial_TaskItem("Initialize", InitializeAlbumsTab));

            genresStatsTab = new StatsCategoryTab("Top Genres", StatsTabIconsStyleKey.YouTubeTopGenres,
                Array.Empty<StatsContentTab>(), new Initial_TaskItem("Initialize", InitializeGenresTab));

            #endregion Stats

            var playlistsTab = new PlaylistTabViewModelBase(m, AppTabs.Playlists, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandPlaylistTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var savedPlaylistsTab = new PlaylistTabViewModelBase(m, AppTabs.SavedPlaylists, LwTabIconKey.SavedPlaylistIcon, 
				playlistTransfer, commandSavedTab,
                new Initial_TaskItem("Reload", Initial_Update_Featured), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var albumsTab = new AlbumTabViewModelBase(m, AppTabs.Albums, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
                new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var tracksTab = new TrackTabViewModelBase(m, AppTabs.Songs, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
                new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            var libraryArtistsTab = new ArtistTabViewModelBase(m, AppTabs.FollowedArtists, LwTabIconKey.ArtistIcon, 
				artistsTransfer, commandArtistsTab,
                new Initial_TaskItem("Reload", Initial_Update_LibraryArtists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var artistsTab = new ArtistTabViewModelBase(m, AppTabs.Subscriptions, LwTabIconKey.ArtistIcon, 
				artistsTransfer, commandSubscriptionsTab,
                new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var uploadsTab = new TrackTabViewModelBase(m, AppTabs.Uploads, LwTabIconKey.UploadsTrackIcon, 
				trackTransfer, commandUploadsTab,
                new Initial_TaskItem("Reload", Initial_Update_Uploads), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            var youMightAlsoLikeTab = new TrackTabViewModelBase(m, AppTabs.YouMightAlsoLike, LwTabIconKey.YouMightAlsoLikeTrackIcon, 
				trackTransfer, commandTracksTab,
                new Initial_TaskItem("Reload", Initial_Update_YouMightAlsoLike), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            var recommendedPlaylistsTab = new PlaylistTabViewModelBase(m, AppTabs.Recommended, LwTabIconKey.RecommendedPlaylistIcon, 
				playlistTransfer, commandSavedTab,
                new Initial_TaskItem("Reload", Initial_Update_RecommendedPlaylists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var mixesTab = new PlaylistTabViewModelBase(m, AppTabs.MixedForYou, LwTabIconKey.MixedForYouPlaylistIcon, 
				playlistTransfer, commandSavedTab,
                new Initial_TaskItem("Reload", Initial_Update_Mixes), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var radiosTab = new PlaylistTabViewModelBase(m, AppTabs.RecommendedRadios, LwTabIconKey.DiscoverWeeklyTrackIcon, 
				playlistTransfer, commandSavedTab,
                new Initial_TaskItem("Reload", Initial_Update_Radios), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));            
            
            var listenAgainTab = new TrackTabViewModelBase(m, "Listen Again", LwTabIconKey.UserMonthSongsTrackIcon, 
				playlistTransfer, commandSavedTab,
                new Initial_TaskItem("Reload", Initial_Update_ListenAgain), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));           
            
            var quickPicksTab = new TrackTabViewModelBase(m, "Quick Picks", LwTabIconKey.TrackIcon, 
				playlistTransfer, commandSavedTab,
                new Initial_TaskItem("Reload", Initial_Update_QuickPicks), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            _statsTab = new StatsViewModelBase(m, AppTabs.Stats,
                LwTabStyleKey.ItemStyleStats, LwTabIconKey.StatsIcon,
                new[] { artistsStatsTab, tracksStatsTab, albumsStatsTab, genresStatsTab },
                EmptyTransfersBase, EmptyCommandsBase,
                new Initial_TaskItem("Reload", Initial_Update_Stats),
                EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            #endregion Tabs

            Tabs.Add(playlistsTab);
            Tabs.Add(savedPlaylistsTab);
            Tabs.Add(albumsTab);
            Tabs.Add(tracksTab);
            Tabs.Add(libraryArtistsTab);
            Tabs.Add(artistsTab);
            Tabs.Add(uploadsTab);
            Tabs.Add(mixesTab);
            Tabs.Add(radiosTab);
            Tabs.Add(listenAgainTab);
            Tabs.Add(quickPicksTab);
            Tabs.Add(youMightAlsoLikeTab);
            Tabs.Add(recommendedPlaylistsTab);
            Tabs.Add(_statsTab);

            HighlightableCommands = new()
            {
                [playlistsTab] = new Command_TaskItem[] { commandBarCreateSmartPlaylistCommand }
            };

            IHighlightableCommands highlightableCommands = this;
            playlistsTab.StoppedLoading += highlightableCommands.OnTabStoppedLoading;
        }

		#endregion Constructors

		#region InitialUpdate

        public override Task<bool> Initial_Update_Tracks(bool forceUpdate = false)
        {
            return InitialUpdateBuilder(YouTubeModel.GetLibraryTracks, SelectedTab, forceUpdate);
        }

        public async Task<bool> Initial_Update_Stats(bool forceUpdate = false)
        {
            if (forceUpdate)
            {
                foreach (var tab in _statsTab.Tiles)
                {
                    tab.AllTabs.Clear();
                    tab.SelectedContentTab = emptyContentTab;
                }

                await _statsTab.SelectedCategoryTab.InitialInitialization.Initial_DoWork(true);
            }

            return await _statsTab.SelectedCategoryTab.SelectedContentTab.InitialMethod.Initial_DoWork(forceUpdate);
        }

        public Task<bool> Initial_Update_StatsArtists(string recapId, bool forceUpdate = false)
        {
            return InitialUpdateBuilder(() => YouTubeModel.GetTopArtists(recapId), _statsTab.SelectedCategoryTab.SelectedContentTab, forceUpdate);
        }

        public Task<bool> Initial_Update_StatsTracks(string recapId, bool forceUpdate = false)
        {
            return InitialUpdateBuilder(() => YouTubeModel.GetTopTracks(recapId), _statsTab.SelectedCategoryTab.SelectedContentTab, forceUpdate);
        }

        public Task<bool> Initial_Update_StatsGenres(string recapId, bool forceUpdate = false)
        {
            return InitialUpdateBuilder(() => YouTubeModel.GetTopGenres(recapId), _statsTab.SelectedCategoryTab.SelectedContentTab, forceUpdate);
        }

        public Task<bool> Initial_Update_StatsAlbums(string recapId, bool forceUpdate = false)
        {
            return InitialUpdateBuilder(() => YouTubeModel.GetTopAlbums(recapId), _statsTab.SelectedCategoryTab.SelectedContentTab, forceUpdate);
        }

        private Task InitializeArtistsTab(bool forceUpdate)
            => InitializeTab(artistsStatsTab, LwTabStyleKey.ItemStyleArtist, Initial_Update_StatsArtists);

        private Task InitializeTracksTab(bool forceUpdate) 
            => InitializeTab(tracksStatsTab, LwTabStyleKey.ItemStyleTrack, Initial_Update_StatsTracks);

        private Task InitializeGenresTab(bool forceUpdate) 
            => InitializeTab(genresStatsTab, LwTabStyleKey.ItemStyleGenre, Initial_Update_StatsGenres);

        private Task InitializeAlbumsTab(bool forceUpdate) 
            => InitializeTab(albumsStatsTab, LwTabStyleKey.ItemStyleAlbum, Initial_Update_StatsAlbums);

        private async Task InitializeTab(StatsCategoryTab tab, LwTabStyleKey style, Func<string, bool, Task<bool>> initialMethod)
        {
            var recaps = await YouTubeModel.GetRecaps();

            var reloadCommand = new List<Command_TaskItem> { new ReloadCommand(Command_Reload, CommandTaskType.CommandBar) };

            if (!recaps.Any())
                return;

            tab.AllTabs.Clear();

            foreach (var recap in recaps)
            {
                tab.AllTabs.Add(new StatsContentTab(MainViewModel, recap.Key,
                    new Initial_TaskItem("Reload", forceUpdate => initialMethod(recap.Value, forceUpdate)),
                    new(),
                    reloadCommand,
                    style));
            }

            tab.SelectedContentTab = tab.AllTabs.First();
        }

        public Task<bool> Initial_Update_LibraryArtists(bool forceUpdate = false)
        {
            return InitialUpdateBuilder(YouTubeModel.GetLibraryArtists, SelectedTab, forceUpdate);
        }

        public Task<bool> Initial_Update_Mixes(bool forceUpdate = false)
        {
            return InitialUpdateBuilder(YouTubeModel.GetMixes, SelectedTab, forceUpdate);
        }

        public Task<bool> Initial_Update_Radios(bool forceUpdate = false)
        {
            return InitialUpdateBuilder(YouTubeModel.GetRecommendedRadios, SelectedTab, forceUpdate);
        }       

        public Task<bool> Initial_Update_ListenAgain(bool forceUpdate = false)
        {
            return InitialUpdateBuilder(YouTubeModel.GetListenAgain, SelectedTab, forceUpdate);
        }        

        public Task<bool> Initial_Update_QuickPicks(bool forceUpdate = false)
        {
            return InitialUpdateBuilder(YouTubeModel.GetQuickPicks, SelectedTab, forceUpdate);
        }

		#endregion InitialUpdate

		#region OpeningMethods

        private Task CommandArtist_Open(object parameters)
        {
            var arg = parameters as List<MusConvModelBase>;
            var artist = arg.First() as MusConvArtist;

            OpenUrlExtension.OpenUrl($"{ArtistDirectUrl}{artist.Id[4..]}");
            return Task.CompletedTask;
        }

		#endregion OpeningMethods

		#region TransferMethods

        public override async Task TransferTrack_Send(List<SearchResultItem<MusConvPlayList, List<MusConvTrackSearchResult>>> result, int arg2, IProgress<ReportCount> progressReport, CancellationToken token)
        {
            var libraryTracks = await (ModelTransferTo as YoutubeMusicModel).GetLibraryTracks();

            var newResult = new List<SearchResultItem<MusConvPlayList, List<MusConvTrackSearchResult>>>(result);

            foreach (var key in result)
            {
                var searchResult = key.SearchResults.FirstOrDefault();
                if (searchResult == null) continue;

                var validResult = searchResult.ResultItems.FirstOrDefault(x =>
                    !string.IsNullOrEmpty(x.GetAdditionalProperty(PropertyType.FeedbackToken)));

                if (validResult is null || libraryTracks.Any(x => x.Id == validResult.Id))
                    newResult.Remove(key);
            }

            await base.TransferTrack_Send(newResult, arg2, progressReport, token);
        }

        public override async Task<MusConvTrackSearchResult> Transfer_Search(int index, MusConvTrack track,
           IProgress<ReportCount> arg3, CancellationToken token)
        {
            MusConvTrackSearchResult result = default;
            try
            {
                var search = $"\"{track.Title}\" by \"{track.Artist}\"";
                arg3.Report(new ReportCount(index, $"Searching: {search} ", ReportType.Searching));
                token.ThrowIfCancellationRequested();

                if (SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.SearchUploadsYoutubeMusic))
                {
                    result = Sort.Sort.DeleteNotValidTracks(new MusConvTrackSearchResult(track, await ModelTransferTo.SearchTrack(new MusConvTrack(){Title = search + "uploads" }, token) ?? new List<MusConvTrack>()), MainViewModel.SettingsVM);
                }

                if (result?.ResultItems == null || result.ResultItems.Count == 0)
                {
                    result = await DefaultSearchAsync(track, token);
                }

                if (result?.ResultItems == null || result.ResultItems.Count == 0)
                {
                    result = Sort.Sort.DeleteNotValidTracks(new MusConvTrackSearchResult(track, await ModelTransferTo.SearchTrack(new MusConvTrack(){Title = search + "video" }, token) ?? new List<MusConvTrack>()), MainViewModel.SettingsVM);
                }
            }
            catch (Exception ex)
            {
                MusConvLogger.LogFiles(ex);
            }

            return result ?? new MusConvTrackSearchResult(track);
        }

		#endregion TransferMethods
	}
}