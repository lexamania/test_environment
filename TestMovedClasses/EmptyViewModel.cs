using System;
using System.Collections.Generic;
using MusConv.MessageBoxManager.Enums;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.Api;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class EmptyViewModel : SectionViewModelBase
	{
		#region Constructors

		public EmptyViewModel(MainViewModelBase m) : base(m)
		{
			RegState = RegistrationState.Unlogged;
			CurrentVMType = VmType.Service;
			AddAllTabs(m);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				if (MusConvLogin.IsTrial)
				{
					PleaseUpgrade();
				}
				else
				{ 
					await  Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}

			return false;
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			await ShowError(MusConvConfig.AuthorizationError, ButtonEnum.RightOk);
		}

		#endregion AuthMethods

		#region TransferMethods

		public override Task Transfer_SaveInTo(params object[] items) => IsServiceSelectedAsync();

		#endregion TransferMethods

		#region InnerMethods

		private void AddAllTabs(MainViewModelBase m)
		{
			#region TransferTasks

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true)
			};
			var albumTransfer = new List<TaskBase_TaskItem>
			{
				new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send,  m, true)
			};

			#endregion TransferTasks

			#region Tabs

			var playlistsTab = new PlaylistTabViewModelBase(m, AppTabs.Playlists, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, EmptyCommandsBase,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));
			
			var albumsTab = new AlbumTabViewModelBase(m, AppTabs.Albums, LwTabIconKey.AlbumIcon, 
				albumTransfer, EmptyCommandsBase,
				new Initial_TaskItem("Reload", Initial_Update_Album), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));
			var tracksTab = new TrackTabViewModelBase(m, AppTabs.Tracks, LwTabIconKey.TrackIcon, 
				trackTransfer, EmptyCommandsBase,
				new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

			Tabs.Add(playlistsTab);
			Tabs.Add(albumsTab);
			Tabs.Add(tracksTab);
		}

		#endregion InnerMethods
	}
}