using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class QubMusiqueViewModel : SectionViewModelBase
	{
		#region Fields

		public bool needSubscrib = true;
		public bool needRedirect = true;
		public bool needLogout = false;
		public bool needRedirectAfterLogout = false;

		#endregion Fields

		#region Constructors

		public QubMusiqueViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Qub Musique";
			SourceType = DataSource.QubMusique;
			LogoKey = LogoStyleKey.QubMusiqueLogo;
			SmallLogoKey = LogoStyleKey.QubMusiqueLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.QubMusiqueSideLogo;
			Url = "https://musique.qub.ca/bibliotheque";
			BaseUrl = "https://musique.qub.ca/";

			Model = new QubMusiqueModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandTracksTab = new List<Command_TaskItem>
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandTracks = new List<Command_TaskItem>
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab, 
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));
			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			RegState = RegistrationState.Unlogged;
			Model.IsAuthorized = false;
			Model = null;
			needLogout = true;
			foreach (var item in Tabs)
			{
				item.MediaItems.Clear();
			}
			await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
			return true;
		}

		public async override Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				if (Model.IsAuthorized)
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
					return false;
				}
				else
				{
					MainViewModel.NeedLogin = this;
					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
					return true;
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		public override async Task Web_NavigatingAsync(object key, object html)
		{
			var selectedTab = SelectedTab;
			selectedTab.Loading = true;
			if(Model is not null)
				await (Model as QubMusiqueModel)?.Select((string)key, (string)html);
			await InitialUpdateForCurrentTab();

			if (selectedTab.Loading)
				selectedTab.Loading = false;
		}

		#endregion AuthMethods

		#region InitialUpdate

		public async override Task<bool> Initial_Update_Tracks(bool forceUpdate = false)
		{
			var res = await InitialUpdateBuilder(Model.GetFavorites, SelectedTab, forceUpdate);
			if (SelectedTab.MediaItems.Count == 0)
				SelectedTab.Loading = true;
			return res;
		}

		#endregion InitialUpdate

		#region InnerMethods

		public void SetUser(string html)
		{
			if (Model is null)
				Model = new QubMusiqueModel();
			//UserEmail = QubMusiqueModel.GetUserMail(html);
			Model.IsAuthorized = true;
		}

		#endregion InnerMethods
	}
}