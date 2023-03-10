using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusConv.Abstractions;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.Sentry.Extensions;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Helper.Parsers;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class RssViewModel : FileManagerViewModelBase
	{
		#region Constructors

		public RssViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Rss";
			Model = new RssModel();
			SourceType = DataSource.Rss;
			LogoKey = LogoStyleKey.RssLogo;
			SideLogoKey = LeftSideBarLogoKey.RssSideLogo;
			_manager = new RssServiceManager();
			_supportedFormats = new() { "rss", "xml" };

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ImportM3UFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			#endregion Commands
			
			var transferPlaylist = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, null, null, m)
			};

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				transferPlaylist, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks);

			Tabs.Add(playlistsTab);
		}

		#endregion Constructors

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] parameters)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			var playlists = parameters[0] as List<MusConvPlayList>;

			try
			{
				OpenFolderDialogWrapper.InnerFileDialog.Title = Texts.SelectFolderToSave;
				var folderPath = await OpenFolderDialogWrapper.ShowAsync();

				if (string.IsNullOrWhiteSpace(folderPath))
					return;

				await SavePlaylistsAsRss(playlists, folderPath);
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
			}
		}

		#endregion TransferMethods

		#region InnerMethods

		private async Task SavePlaylistsAsRss(List<MusConvPlayList> playlists, string folderPath)
		{
			foreach(var playlist in playlists)
			{
				var playlistPath = FileCreationManager.ConfigureFilePath(playlist.Title, folderPath, "rss");
				
				await using var sw = new StreamWriter(File.Open(playlistPath, FileMode.OpenOrCreate), Encoding.UTF8);
				await sw.WriteLineAsync(RssParser.CreateRss(playlist));

				var searchItems = playlist.AllTracks
					.Select(result => new MusConvTrackSearchResult(result, new List<MusConvTrack> { result }))
					.ToList();

				MainViewModel.ResultVM.ResultSearchOfTracksItems.Add(new(playlist, searchItems));
			}

			var message = $"You can find Rss files in folder: {folderPath}";
			await ShowMessage($"Rss files were created. {Environment.NewLine}{message}", Icon.Info);
		}

		#endregion InnerMethods
	}
}