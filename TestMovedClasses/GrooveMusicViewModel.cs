using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class GrooveMusicViewModel : SectionViewModelBase
	{
		#region Constructors

		public GrooveMusicViewModel(MainViewModelBase m) : base(m)
		{
			if (IsAvailable())
			{
				Title = "Groove Music";
				SourceType = DataSource.GrooveMusic;
				LogoKey = LogoStyleKey.GrooveLogo;
				SmallLogoKey = LogoStyleKey.GrooveLogoSmall;
				SideLogoKey = LeftSideBarLogoKey.GrooveSideLogo;
				CurrentVMType = VmType.Service;
				RegState = RegistrationState.Needless;
				Model = new GrooveModel();
				IsHelpVisible = true;
				IsSuitableForAutoSync = false;
				NavigateHelpCommand = ReactiveCommand.Create(() => m.NavigateTo(NavigationKeysChild.GrooveHelpPage));
			}

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new EditCommand(Command_Edit, CommandTaskType.DropDownMenu),
				new EditCommand(Command_Edit, CommandTaskType.CommandBar),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
			{
				new EditCommand(Command_Edit, CommandTaskType.DropDownMenu),
				new EditCommand(Command_Edit, CommandTaskType.CommandBar),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.TrackIcon,
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks);

			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks);

			var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks);

			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase);

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(artistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region CheckingProperties

		public virtual bool IsAvailable() => 
			Environment.OSVersion.VersionString.Contains("Windows");

		#endregion CheckingProperties

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent).ConfigureAwait(false);
			try
			{
				await InitialUpdateForCurrentTab();
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				return false;
			}
			
			return true;
		}

		#endregion AuthMethods

		#region Commands

		public override Task Command_Help(object arg1, CommandTaskType commandTaskType)
		{
			MainViewModel.NavigateTo(NavigationKeysChild.GrooveHelpPage);
			return Task.CompletedTask;
		}

		#endregion Commands
	}
}