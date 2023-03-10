using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Interfaces;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Models.MusicService.Base;
using MusConv.ViewModels.Settings;
using MusConv.ViewModels.Sort;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MusConv.ViewModels.ViewModels.WebViewViewModels
{
    // displays data from https://www.youtube.com/feed/library
    public class YoutubeViewModel : YouTubeViewModelBase, IHighlightableCommands
	{
		#region Fields

        public Dictionary<TabViewModelBase, Command_TaskItem[]> HighlightableCommands { get; }

		#endregion Fields

		#region Constructors

        public YoutubeViewModel(MainViewModelBase m) : base(m)
        {
            Title = "YouTube";
            SourceType = DataSource.Youtube;
            LogoKey = LogoStyleKey.YoutubeLogo;
            SmallLogoKey = LogoStyleKey.YoutubeLogoSmall;
            SideLogoKey = LeftSideBarLogoKey.YoutubeSideLogo;
            CurrentVMType = VmType.Service;
            BaseUrl = "https://youtube.com/";
            ArtistDirectUrl = "https://youtube.com/channel/";
            AlbumDirectUrl = "https://youtube.com/playlist?list=";
            SearchUrl = "https://youtube.com/search?q=";
            Model = CreateYouTubeModel();

            NavigateHelpCommand = ReactiveCommand.Create(() =>
                m.NavigateTo(NavigationKeysChild.YoutubeHelp));

            #region Commands

            var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewAlbumCommand(CommandTrack_OpenAlbum),
                new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
                new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar),
                new YouMightAlsoLikeCommand(Command_YouMightAlsoLike_Open),
                new RecommendPlaylistCommand(Command_RecommendedPlaylists_Open, CommandTaskType.DropDownMenu),
            };

            var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewAlbumCommand(CommandTrack_OpenAlbum),
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
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewAlbumCommand(CommandTrack_OpenAlbum),
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
            	new UploadCommand(Command_Upload, CommandTaskType.CommandBar),
			};

            var commandBarCreateSmartPlaylistCommand = new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.CommandBar);
            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandPlaylist_Open),
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
                new ReplaceInPlaylistCommand(Command_ReplaceInPlaylist, CommandTaskType.DropDownMenu),
                new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.DropDownMenu),
                new SyncPlaylistsCommand(Command_SyncPlaylist, CommandTaskType.CommandBar),
                new ExportAsCommand(Command_Export, CommandTaskType.CommandBar),
                new SortCommand(Command_Sort, CommandTaskType.CommandBar),
                new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.CommandBar),
                new ReceiptModeCommand(Command_ReceiptMode, CommandTaskType.DropDownMenu),
                commandBarCreateSmartPlaylistCommand,
                new CreateSmartPlaylistCommand(Command_CreateSmartPlaylist, CommandTaskType.DropDownMenu),
            };

            var commandSavedTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandPlaylist_Open),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
                new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.DropDownMenu),
                new AddToPlaylistCommand(Command_AddToPlaylist, CommandTaskType.CommandBar),
            };

            var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_OpenArtist),
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
            var transfer = new List<TaskBase_TaskItem>
            {
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};
            var artistsTransfer = new List<TaskBase_TaskItem>
            {
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};

            #endregion Transfer

            #region Tabs

            var createdPlaylistsTab = new PlaylistTabViewModelBase(m, AppTabs.CreatedPlaylists, LwTabIconKey.CreatedPlaylistIcon, 
				transfer, commandPlaylistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Playlists_StrategyBased), commandPlaylistTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var savedPlaylistsTab = new PlaylistTabViewModelBase(m, AppTabs.SavedPlaylists, LwTabIconKey.SavedPlaylistIcon, 
				transfer, commandSavedTab,
                new Initial_TaskItem("Reload", Initial_Update_Featured), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
                new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
                new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistsTransfer, commandArtistsTab,
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
				transfer, commandSavedTab,
                new Initial_TaskItem("Reload", Initial_Update_RecommendedPlaylists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));
            
            var subscriptionsTab = new TrackTabViewModelBase(m, AppTabs.Subscriptions, LwTabIconKey.UserMonthSongsTrackIcon, 
				trackTransfer, commandTracksTab,
                new Initial_TaskItem("Reload", Initial_Update_Subscriptions), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            #endregion Tabs

            Tabs.Add(createdPlaylistsTab);
            Tabs.Add(savedPlaylistsTab);
            Tabs.Add(albumsTab);
            Tabs.Add(tracksTab);
            Tabs.Add(artistsTab);
            Tabs.Add(uploadsTab);
            Tabs.Add(youMightAlsoLikeTab);
            Tabs.Add(subscriptionsTab);
            Tabs.Add(recommendedPlaylistsTab);

            HighlightableCommands = new()
            {
                [createdPlaylistsTab] = new Command_TaskItem[] { commandBarCreateSmartPlaylistCommand }
            };

            IHighlightableCommands highlightableCommands = this;
            createdPlaylistsTab.StoppedLoading += highlightableCommands.OnTabStoppedLoading;
        }

		#endregion Constructors

		#region InitialUpdate

        private Task<bool> Initial_Update_Subscriptions(bool forceUpdate)
        {
            return InitialUpdateBuilder(YouTubeModel.GetSubscriptions, SelectedTab, forceUpdate);
        }

		#endregion InitialUpdate

		#region TransferMethods

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

                if (result?.ResultItems != null && result.ResultItems.Count != 0)
                    return result;

                result = await DefaultSearchAsync(track, token);
                if (result?.ResultItems != null && result.ResultItems.Count != 0)
                    return result;

                // searching in ytm with SortTracksExtension
                var tracks = await ModelTransferTo.SearchTrack(new MusConvTrack(){Title = search }, token) ?? new List<MusConvTrack>();
                if (tracks.Count != 0)
                {
                    result = Sort.Sort.DeleteNotValidTracks(
                        new MusConvTrackSearchResult(track, tracks.Copy()),
                        MainViewModel.SettingsVM,
                        SortTracksExtension.CheckDuration | SortTracksExtension.ClearWhiteSpaces);
                    if (result?.ResultItems != null && result.ResultItems.Count != 0)
                        return result;

                    result = Sort.Sort.DeleteNotValidTracks(
                        new MusConvTrackSearchResult(track, tracks),
                        MainViewModel.SettingsVM,
                        SortTracksExtension.CheckDuration | SortTracksExtension.IngoreAlbums |
                        SortTracksExtension.IngoreArtist);
                    if (result?.ResultItems != null && result.ResultItems.Count != 0)
                        return result;
                }

                // searching video
                var videoTracks = await ModelTransferTo.SearchTrack(new MusConvTrack(){Title = search + "video" }, token) ?? new List<MusConvTrack>();
                if (videoTracks.Count == 0)
                {
                    search = $"{track.Title} by {track.Artist}";
                    videoTracks = await ModelTransferTo.SearchTrack(new MusConvTrack(){Title = search + "video" }, token) ?? new List<MusConvTrack>();
                }

                result = Sort.Sort.DeleteNotValidTracks(
                    new MusConvTrackSearchResult(track, videoTracks.Copy()),
                    MainViewModel.SettingsVM,
                    SortTracksExtension.MinimizeAccuracy | SortTracksExtension.CheckDuration);
                if (result?.ResultItems != null && result.ResultItems.Count != 0)
                    return result;

                result = Sort.Sort.DeleteNotValidTracks(
                    new MusConvTrackSearchResult(track, videoTracks.Copy()),
                    MainViewModel.SettingsVM,
                    SortTracksExtension.MinimizeAccuracy);
                if (result?.ResultItems != null && result.ResultItems.Count != 0)
                    return result;

                // trying to replace artist as title (full video name), because artist is name of video's channel
                foreach (var videoTrack in videoTracks)
                    videoTrack.Artist = videoTrack.Title;

                result = Sort.Sort.DeleteNotValidTracks(
                    new MusConvTrackSearchResult(track, videoTracks),
                    MainViewModel.SettingsVM,
                    SortTracksExtension.MinimizeAccuracy);
            }
            catch (Exception ex)
            {
                MusConvLogger.LogFiles(ex);
            }

            return result ?? new MusConvTrackSearchResult(track);
        }

		#endregion TransferMethods

		#region InnerMethods

        // Method to check YouTube authorization from Web Url service
        public async Task<bool> IsServiceSelectedAsyncWebUrl()
        {
            try
            {
                IsSending = false;

                if (!Model.IsAuthenticated())
                {
                    if (await IsAnyAccountAuthenticated() ||
                        SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
                    {
                        await InitialAuthorization().ConfigureAwait(false);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MusConvLogger.LogFiles(ex);
            }

            return true;
        }

		#endregion InnerMethods
	}
}