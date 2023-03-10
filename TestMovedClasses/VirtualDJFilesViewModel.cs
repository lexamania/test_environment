using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Settings;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class VirtualDJFilesViewModel : FileManagerViewModelBase
	{
		#region Constructors

		public VirtualDJFilesViewModel(MainViewModelBase m) : base(m)
		{
			Title = "VirtualDJFile";
			Model = new VirtualDJFilesModel();
			SourceType = DataSource.VirtualDJFiles;
			LogoKey = LogoStyleKey.VirtualDJFilesLogo;
			SideLogoKey = LeftSideBarLogoKey.VirtualDJFilesSideLogo;
			IsSuitableForAutoSync = false;

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new ImportM3UFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks);

			Tabs.Add(playlistsTab);
		}

		#endregion Constructors

		#region Commands

		public override async Task ImportFileCommand(object obj, CommandTaskType commandTaskType)
		{
			SelectedTab.Loading = true;
			SelectedTab.LoadingText = MusConvConfig.PlayListLoading;

			try
			{
				List<string> filePaths = new();

				if (SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.SelectFolderInsteadOfMultipleFiles))
				{
					var filter = @$"(?i)^.+(.TXT|.M3U|.HTML|.CSV)$";

					(_, filePaths) = await GetFilesFromFolder($"Please select folder with files exported from VirtualDJ", filter);
				}
				else
				{
					(_, filePaths) = await GetFilesFromFolder("", $"Please select files exported from VirtualDJ", _supportedFormats);
				}

				if (filePaths is null)
				{
					SelectedTab.Loading = false;
					return;
				}

				foreach (var item in filePaths)
				{
					_manager.AddPath(item, _manager.GetPath(LwTabStyleKey.ItemStylePlaylist));
				}

				await InitialUpdateForCurrentTab(true);
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				Initial_Setup();
			}
		}

		#endregion Commands
	}
}