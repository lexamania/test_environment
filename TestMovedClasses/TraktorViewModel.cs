using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Avalonia.Threading;
using Sentry;
using MusConv.Abstractions;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Settings;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using static MusConv.ViewModels.Models.TrackerManager;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.Api;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class TraktorViewModel : FileManagerViewModelBase
	{
		#region Constructors

		public TraktorViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Traktor";
			SourceType = DataSource.Traktor;
			CurrentVMType = VmType.FileVM;
			LogoKey = LogoStyleKey.TracktorLogo;
			SideLogoKey = LeftSideBarLogoKey.TraktorSideLogo;
			RegState = RegistrationState.Needless;
			//can`t be destination for autosync, need to implement method AddTracksToPlaylist for all file services
			IsSuitableForAutoSync = false;
			IsReplaceAvailable = false;
			Model = new TraktorModel();

			#region Commands
			
			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ImportNmlFileCommand(ImportFileCommand,CommandTaskType.CommandBar),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ImportNmlFileCommand(ImportFileCommand,CommandTaskType.CommandBar),
				new HelpCommand(Command_Help, CommandTaskType.CommandBar),
            };

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.DropDownMenu),
				new AddToPlaylistCommand(Command_Track_AddToPlaylist, CommandTaskType.CommandBar)
			};

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab, 
				new Initial_TaskItem("Load .nml", Initial_Update_Playlists), commandTracks);
			var tracksTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab, 
				new Initial_TaskItem("Load .nml", Initial_Update_Tracks), EmptyCommandsBase);

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(tracksTab);
		}

		#endregion Constructors

		#region Commands

		public override async Task ImportFileCommand(object obj, CommandTaskType commandTaskType)
		{
			OpenFileDialogWrapper.InnerFileDialog.Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			OpenFileDialogWrapper.InnerFileDialog.Filters = new List<Avalonia.Controls.FileDialogFilter>
			{
				new Avalonia.Controls.FileDialogFilter
				{
					Name="", //workaround because avalonia somewhy throw exception if you will not set it
					Extensions= _supportedFormats
				}
			};
			OpenFileDialogWrapper.InnerFileDialog.Title = "Please select Traktor library file"
														  + Environment.NewLine +
														  "To get it, in your Traktor program, please right click on your 'Track Collection', then 'Export the Collection'";
			OpenFileDialogWrapper.InnerFileDialog.AllowMultiple = true;
			var result = await Dispatcher.UIThread.InvokeAsync(OpenFileDialogWrapper.ShowAsync).ConfigureAwait(false);
			OpenFileDialogWrapper.InnerFileDialog.Title = "";

			foreach (var path in result)
			{
				_manager.AddPath(path, _manager.GetPath(SelectedTab.Lwstyle));
			}

			await InitialUpdateForCurrentTab(true);
		}

		#endregion Commands

		#region TransferMethods

		public override async Task Transfer_SaveInTo(object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);

			await Transfer_DoWork(items[0]);
		}

		public override async Task Transfer_DoWork(params object[] parameters)
		{
			try
			{
				Sort.Sort.DestinationService = SourceType;
				
				SentrySdk.AddBreadcrumb(message: $"User started transfer to {Title}, transferring to {MainViewModel.TaskQItems.Last().Lw.ToString().Substring(9)} tab",
					level: BreadcrumbLevel.Info);

				MainViewModel.ResultVM.TrialCount = TransferTracker.Tracker.HowMuchTodayTransferred;
				if (MusConvLogin.IsTrial && TransferTracker.Tracker.HowMuchTodayTransferred >= MusConvConfig.TracksLimit &&
					TransferTracker.Tracker.LastDayTransferred.Day == DateTime.Now.Date.Day)
				{
					await TrialPeriod(parameters);
					return;
				}

				var re = MainViewModel.TaskQItems.Where(t => t.Progress == 1).ToList();

				await Dispatcher.UIThread.InvokeAsync(() =>
				{
					foreach (var r1 in re) MainViewModel.TaskQItems.Remove(r1);
				});
				if (MainViewModel.TaskQItems.Count > 1)
				{
					var temitem = MainViewModel.TaskQItems.Last();
					await Dispatcher.UIThread.InvokeAsync(() =>
					{
						MainViewModel.TaskQItems.Clear();
						MainViewModel.TaskQItems.Add(temitem);
					});
				}
				var taskItemsToRemove = new List<TaskBase_TaskItem>();
				foreach (var task in MainViewModel.TaskQItems.Where(t => !t.IsWorking))
				{
					try
					{
						if (task.Token.IsCancellationRequested == true)
						{
							return;
						}
						task.Messager = () =>
						{
							MainViewModel.Message = task.Message;
							MainViewModel.Progress = task.Progress;
						};

						if (parameters[0] is List<MusConvPlayList> playlists)
						{
							if (SettingsViewModel.IsSettingOptionRequired(SettingsOptionType.ScanLocalFolder)
								|| !playlists.Any(p => p.AllTracks.Any(t => !string.IsNullOrWhiteSpace(t?.Path))))
							{
								parameters[0] = await MatchingPlaylist(playlists);
							}
						}
						else
						{
							return;
						}

						await task.DoWork(parameters[0] as IEnumerable<MusConvItemBase>, IsAutoRetryRequested, false);
					}
					catch (Exception e)
					{
						MusConvLogger.LogFiles(e);
						if (e.HResult == -2146233029)
						{
							taskItemsToRemove.Add(task);
						}
					}
				}

				foreach (var item in taskItemsToRemove)
				{
					await Dispatcher.UIThread.InvokeAsync(() => MainViewModel.TaskQItems.Remove(item));
				}

				if (MusConvLogin.IsTrial && TransferTracker.Tracker.HowMuchTodayTransferred >= MusConvConfig.TracksLimit &&
					TransferTracker.Tracker.LastDayTransferred.Day == DateTime.Now.Date.Day)
				{
					await TrialPeriod(parameters);
					return;
				}

				MainViewModel.ResultVM.WasFirstTransfer = true;
				IsResultsWindowAlreadyShowed = false;
			}
			catch (Exception e)
			{
				await ShowError(e.Message);
			}
		}

		public override async Task<MusConvTrackSearchResult> Transfer_Search(int index, MusConvTrack track, IProgress<ReportCount> arg3, CancellationToken token)
		{
			try
			{
				var search = $"\"{track.Title}\" by \"{track.Artist}\"";
				arg3.Report(new ReportCount(index, $"Searching: {search} ", ReportType.Searching));
				token.ThrowIfCancellationRequested();

				var result = new MusConvTrackSearchResult(track) { ResultItems = new() };
				if (string.IsNullOrEmpty(track.Path))
				{
					result.ResultItems = await Model.SearchTrack(track, token).ConfigureAwait(false);

					return Sort.Sort.DeleteNotValidTracks(result, MainViewModel.SettingsVM);
				}
				else
				{
					if (File.Exists(track.Path))
					{
						result.ResultItems = new List<MusConvTrack> { track };
					}
				};

				return result;
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				return new MusConvTrackSearchResult(track);
			}
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			try
			{
				MainViewModel.ResultVM.SetPlaylistSearchItem(result);

				if (result.Count > 0)
				{
					OpenFolderDialogWrapper.InnerFileDialog.Title = Texts.SelectFolderToSave;
					var pathToSave = await OpenFolderDialogWrapper.ShowAsync();
					foreach (var resultKey in result.Keys)
					{
						try
						{
							var createModel = new MusConvPlaylistCreationRequestModel(resultKey, resultKey.AllTracks);
							var pl = await (Model as TraktorModel).CreatePlaylist(createModel, pathToSave);
						}
						catch (Exception e) when (e is not KeyNotFoundException)
						{
							MusConvLogger.LogFiles(e);
						}

						progressReport.Report(new ReportCount(resultKey.AllTracks.Count,
						   $"Adding tracks to \"{resultKey}\"",
						   ReportType.Sending));
					}

					var message = $"You can find NML files in folder: {pathToSave}";
					await ShowWarning($"NML files were created. {Environment.NewLine}{message}");
				}

				progressReport.Report(GetPlaylistsReportCount(result));
			}
			catch (Exception e)
			{
				await ShowError(e.Message);
				MusConvLogger.LogFiles(e);
			}
		}

		#endregion TransferMethods

		#region InnerMethods

		private async Task<List<MusConvPlayList>> MatchingPlaylist(List<MusConvPlayList> playlists)
		{
			var result = new List<MusConvPlayList>();
			(string folderPath, List<string> filePaths) = await GetFilesFromFolder(Texts.SelectFolder, Filters.MusicFileExtensionFilter).ConfigureAwait(false);
			if (folderPath is null || filePaths is null)
			{
				return result;
			}

			var localPlaylists = MatchPlaylistsWithLocalMedias(playlists, filePaths, true);
			if (!localPlaylists.Any(playlist => playlist.Tracks.Any(track => track.SimilarLocalTracks.Count > 0)))
			{
				await ShowWarning(Texts.DidntMatchSongsForNml);
				return result;
			}

			result = localPlaylists
				.Select(p => new MusConvPlayList(p.Title, _mapper.TrackMapper.MapTracks(p.Tracks)) { IsSelected = true })
				.ToList();

			return result;
		}

		#endregion InnerMethods
	}
}