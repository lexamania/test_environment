using Avalonia.Controls;
using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using ReactiveUI;
using Sentry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MusConv.Lib.Plex.Models.ResourceModels;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Network.Utils;
using MusConv.Sentry.Extensions;
using MusConv.ViewModels.Settings.EventArgs;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using static MusConv.ViewModels.Models.TrackerManager;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using System.Runtime.InteropServices;
using MusConv.ViewModels.Api;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class PlexViewModel : PlexTidalViewModel
	{
		#region Fields

		private PlexModel PlexModel => Model as PlexModel;
		private KeyValuePair<MusicServiceBase, Resources>? _selectedServer;
		private object _transferItem;
		public bool IsSelectServerButtonActive => SelectedServer != null;
		public KeyValuePair<MusicServiceBase, Resources>? SelectedServer
		{
			get => _selectedServer;
			set
			{
				_selectedServer = value;
				PlexModel.SelectedServer = _selectedServer?.Value;
			}
		}
		public ReactiveCommand<Unit, Unit> Command_ConfirmSelectServer { get; set; }
		private bool _usePlexMetadata;
		private bool _notTransfered;

		#endregion Fields

		#region Constructors

		public PlexViewModel(MainViewModelBase m) : base(m)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
				this.WhenAnyValue(x => x.Model).Subscribe(x =>
					m.SettingsVM.SubscribeToOptionStateChanged(RefreshAddTracksStrategy,
						SettingsOptionType.PlexFastTransfer));
            }
			
			Title = "Plex";
			CurrentVMType = VmType.Service;
			Model = new PlexModel();
			BaseUrl = "https://www.plex.tv/";
			SourceType = DataSource.Plex;
			LogoKey = LogoStyleKey.PlexLogo;
			SideLogoKey = LeftSideBarLogoKey.PlexSideLogo;
			IsServiceVisibleAsSource = true;
			IsHelpVisible = true;
			NavigateHelpCommand = ReactiveCommand
				.Create(() => m.NavigateTo(NavigationKeysChild.PlexHelp));

			Command_ConfirmSelectServer = ReactiveCommand.CreateFromTask(ConfirmSelectServer);

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ImportM3UFileCommand(InitialImportTab, CommandTaskType.CommandBar),
			};

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDeleteTracks_FromPlaylist, CommandTaskType.CommandBar)
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon,
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandPlaylistTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var albumTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var artistTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				EmptyTransfersBase, commandArtistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var trackTab = new TrackTabViewModelBase(m, LwTabIconKey.TrackIcon, 
				EmptyTransfersBase, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistTab);
			Tabs.Add(albumTab);
			Tabs.Add(artistTab);
			Tabs.Add(trackTab);
		}

		#endregion Constructors

		#region InitialUpdate

		public async Task InitialImportTab(object obj, CommandTaskType commandTaskType)
		{
			MainViewModel.NavigateTo(NavigationKeysChild.Content);

			OpenFileDialogWrapper.InnerFileDialog.AllowMultiple = true;
			OpenFileDialogWrapper.InnerFileDialog.Title = "Please select your m3u files";
			OpenFileDialogWrapper.InnerFileDialog.Filters = new List<FileDialogFilter>
			{
				new FileDialogFilter
				{
					Name = "",
					Extensions = new List<string> { "m3u", "M3U", "M3u", "m3U" }
				}
			};

			var m3UFilePaths = (await Dispatcher.UIThread.InvokeAsync(OpenFileDialogWrapper.ShowAsync))?.ToList();

			List<MusConvPlayList> playlists = new();
			if (m3UFilePaths != null && m3UFilePaths.Count > 0)
			{
				try
				{
					foreach (var filePath in m3UFilePaths)
					{
						var content = MyExtensions.ReadFromFile(filePath);
						var title = Path.GetFileNameWithoutExtension(filePath);

						if (content.Contains("#EXTM3U"))
						{
							try
							{
								playlists.Add(M3UParser.ParseExtendedM3U(title, content));
							}
							catch (Exception ex)
							{
								MusConvLogger.LogFiles(ex);
							}
						}
						else
						{
							try
							{
								playlists.Add(M3UParser.Parse(content));
							}
							catch (Exception ex)
							{
								MusConvLogger.LogFiles(ex);
							}
						}

						var playlistTransferTaskItem = new PlaylistTransfer_TaskItem(Tabs
							.FirstOrDefault(t => t.Lwstyle == LwTabStyleKey.ItemStylePlaylist)
							.TransferMethods.FirstOrDefault() as PlaylistTransfer_TaskItem, MainViewModel);

						MainViewModel.TaskQItems.Add(playlistTransferTaskItem);
						Sort.Sort.SourceService = DataSource.M3U;

						playlists.ForEach(pl => pl.IsSelected = true);
					}

					await Transfer_SaveInTo(playlists);
					if (_notTransfered)
					{
						ShowWarning(
							"Looks like your tracks weren't transferred.\nYou need to have media locally or on your Plex servers.");
					}
				}
				catch (Exception ex)
				{
					MusConvLogger.LogFiles(ex);
				}
			}

			SelectedTab.Loading = false;
		}

		#endregion InitialUpdate

		#region SearchMethods

		public override async Task<MusConvTrackSearchResult> DefaultSearchAsync(MusConvTrack track,
			CancellationToken token)
		{
			if (string.IsNullOrEmpty(track.Path))
			{
				return await base.DefaultSearchAsync(track, token);
			}
			
			var result = new MusConvTrackSearchResult(track, new List<MusConvTrack>());

			try
			{
				var searchedTrack = await PlexModel.SearchLocalTrack(track.Path);
				result.ResultItems.Add(searchedTrack);
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
			}

			return result;

		}

		#endregion SearchMethods

		#region TransferMethods

		public override async Task<MusConvTrackSearchResult> Transfer_Search(int index, MusConvTrack track,
			IProgress<ReportCount> arg3, CancellationToken token)
		{
			var search = $"\"{track.Title}\" by \"{track.Artist}\"";
			var result = new MusConvTrackSearchResult(track) { ResultItems = new() };
			
			try
			{
				arg3.Report(new ReportCount(index, $"Searching: {search} ", ReportType.Searching));
				token.ThrowIfCancellationRequested();


				if (_usePlexMetadata || string.IsNullOrEmpty(track.Path))
				{
					result.ResultItems = await Model.SearchTrack(track, token)
						.ConfigureAwait(false);

					return Sort.Sort.DeleteNotValidTracks(result, MainViewModel.SettingsVM);
				}
				
				var searchedTrack = await PlexModel.SearchLocalTrack(track.Path)
					.ConfigureAwait(false);
				
				result.ResultItems = new List<MusConvTrack> { searchedTrack };
				_notTransfered = false;
				
				return result;
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
				_notTransfered = true;
			}

			return result;
		}

		public override async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result,
			int index,
			IProgress<ReportCount> progressReport, CancellationToken token)
		{
			try
			{
				MainViewModel.ResultVM.SetPlaylistSearchItem(result);
				foreach (var resultKey in result.Keys)
				{
					var tracksToSend = result[resultKey]?.Where(x => x?.ResultItems?.Count > 0)
						.Select(x => x.ResultItems?.FirstOrDefault()).ToList() ?? new List<MusConvTrack>();

					if (tracksToSend.Count == 0) continue;

					token.ThrowIfCancellationRequested();
					progressReport.Report(new ReportCount(tracksToSend.Count,
						$"Adding tracks to \"{resultKey}\"",
						ReportType.Sending));

					try
					{
						var createModel = new MusConvPlaylistCreationRequestModel(resultKey, tracksToSend);
						var playlist = await Model.CreatePlaylist(createModel);

						MainViewModel.ResultVM.MediaItemIds.Add(playlist.Id);
					}
					catch (Exception e) when (e is not KeyNotFoundException)
					{
						MusConvLogger.LogFiles(e);
					}
				}

				NavigateToContent();
				progressReport.Report(GetPlaylistsReportCount(result));
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
			}
		}

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			_transferItem = items[0];
			IsSending = true;

			if (!Model.IsAuthenticated())
			{
				if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) ||
					!await IsServiceDataExecuted(data))
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage).ConfigureAwait(false);
				}
			}
			else
			{
				WaitAuthentication.Set();
				IsSending = false;
			}
			
			await WaitAuthentication.WaitAsync();

			try
			{
				MainViewModel.Message = Texts.PlexLoadingServers;
				await LoadUserServers();
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				MusConvLogger.LogFiles(sentryEvent);
				await ShowError(e.Message);
				return;
			}
			finally
			{
				WaitAuthentication.Set();
				MainViewModel.Message = string.Empty;
			}

			if (Servers.Count > 1)
			{
				MainViewModel.NavigateTo(NavigationKeysChild.SelectTransferServer);
				return;
			}

			SelectedServer = Servers.FirstOrDefault();

			await Transfer_DoWork(items[0]);
		}

		public override async Task Transfer_DoWork(params object[] parameters)
		{
			try
			{
				Sort.Sort.DestinationService = SourceType;
				
				SentrySdk.AddBreadcrumb(
					message:
					$"User started transfer to {Title}, transferring to {MainViewModel.TaskQItems.Last().Lw.ToString().Substring(9)} tab",
					level: BreadcrumbLevel.Info);

				MainViewModel.ResultVM.TrialCount = TransferTracker.Tracker.HowMuchTodayTransferred;

				if (MusConvLogin.IsTrial &&
					TransferTracker.Tracker.HowMuchTodayTransferred >= MusConvConfig.TracksLimit &&
					TransferTracker.Tracker.LastDayTransferred.Day == DateTime.Now.Date.Day)
				{
					await TrialPeriod(parameters);
					return;
				}

				IsResultsWindowAlreadyShowed = false;
				TabViewModelBase.IsWrongLogin = false;
				if (!(parameters.Length > 1 && parameters[1] is bool waitAuth == true))
				{
					await WaitAuthentication.WaitAsync();
				}

				TabViewModelBase.IsWrongLogin = true;
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
				foreach (var task in MainViewModel.TaskQItems.Where(t => !t.IsWorking).ToList())
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
							MainViewModel.Message = Texts.PlexFetchingLocalTracks;
							await Task.Run(PlexModel.FetchLocalTracks).ConfigureAwait(false);
							if (playlists.Any(p => p.AllTracks.Any(t => !string.IsNullOrWhiteSpace(t?.Path))))
							{
								_usePlexMetadata = false;
							}
							else if (IsSelectedLocalServer())
							{
								var res = await ShowMessage("Use metadata from Plex to match the songs?",
									ButtonEnum.YesNo, Icon.Info);

								if (res == ButtonResult.Yes)
									_usePlexMetadata = true;
								else
								{
									var matchedPlaylists =
										await GetMatchedPlaylistsWithLocalMedias(
											parameters[0] as List<MusConvPlayList>);

									if (!matchedPlaylists.Any(p =>
											p.AllTracks.Any(t => !string.IsNullOrWhiteSpace(t?.Path))))
									{
										await ShowWarning(Texts.DidntMatchSongs);
										return;
									}

									parameters[0] = matchedPlaylists;
									_usePlexMetadata = false;
								}
							}
							else
							{
								_usePlexMetadata = true;
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

				MainViewModel.ResultVM.WasFirstTransfer = true;

				if (MusConvLogin.IsTrial &&
					TransferTracker.Tracker.HowMuchTodayTransferred >= MusConvConfig.TracksLimit)
				{
					await TrialPeriod(parameters);
					return;
				}
				else if (_notTransfered)
				{
					ShowWarning(
						"Looks like your tracks weren't transferred.\nYou need to have media locally or on your Plex servers.");
				}

				await ShowErrorsAsync();
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				MusConvLogger.LogFiles(sentryEvent);
				await ShowError(PlexTexts.PlexImportError);
			}
		}

		#endregion TransferMethods

		#region InnerMethods

		public async Task ConfirmSelectServer()
		{
			WaitAuthentication.Set();
			NavigateToContent();
			await Transfer_DoWork(_transferItem);
			SelectedServer = null;
		}

		public async Task<List<MusConvPlayList>> GetMatchedPlaylistsWithLocalMedias(List<MusConvPlayList> playlists)
		{
			try
			{
				var filter =
					@"(?i)^.+(.AAC|.MP4|.M4A|.AIF|.AIFF|.AIFC|.DSD|.DSF|.AC3|.FLAC|.GYM|.MID|.MIDI|.APE|.MP1|.MP2|.MP3|.MPC|.MP|.MOD|.OGG|.OPUS|.OFR|.OFS|.PSF|.PSF1|.PSF2|.MINIPSF|.MINIPSF1|.MINIPSF2|.SSF|.MINISSF|.DSF|.MINIDSF|.GSF|.MINIGSF|.QSF|.MINISQF|.S3M|.SPC|.TAK|.VQF|.WAV|.BWAV|.BWF|.VGM|.VGZ|.WV|.WMA|.ASF|.M4P)$";

				(string folderPath, List<string> filePaths) = await GetFilesFromFolder(Texts.SelectFolder, filter);

				if (folderPath is null || filePaths is null)
					return null;

				return await Task.Run(() =>
				{
					var localPlaylists = MatchPlaylistsWithLocalMedias(playlists, filePaths, true);

					return localPlaylists.Select(p => new MusConvPlayList(p.Title, _mapper.TrackMapper.MapTracks(p.Tracks))).ToList();
				});
			}
			catch (Exception e)
			{
				var sentryEvent = SentryEventBuilder.Build(e).WithVisibleTag(true);
				await ShowError(e.Message);
				MusConvLogger.LogFiles(sentryEvent);
			}

			return null;
		}

		public bool IsSelectedLocalServer()
		{
			var localIpAddress = LocalNetworkUtils.GetLocalAddress();
			var selectedServer = SelectedServer.Value.Value;
			return selectedServer.Connections.FirstOrDefault(x => x.Local)?.Address == localIpAddress &&
				   selectedServer.PublicAddressMatches;
		}

		private void RefreshAddTracksStrategy([CanBeNull] object sender, OptionStateChangedEventArgs args)
		{
			if (args.OptionType != SettingsOptionType.PlexFastTransfer)
			{
				return;
			}
			
			PlexModel.SetAddItemStrategy(args.IsChecked);
		}

		#endregion InnerMethods
	}
}