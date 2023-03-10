using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	class RessoViewModel : SectionViewModelBase
	{
		#region Constructors

		public RessoViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Resso";
			RegState = RegistrationState.Needless;
			SourceType = DataSource.Resso;
			LogoKey = LogoStyleKey.RessoLogo;
			SideLogoKey = LeftSideBarLogoKey.RessoSideLogo;
			CurrentVMType = VmType.Service;
			Model = new RessoModel();
			BaseUrl = "https://www.resso.com/in/";

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new ExportAsCommand(Command_Export, CommandTaskType.CommandBar)
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase);

			#endregion Commands

			var playlistTransfer = new List<TaskBase_TaskItem>
			{ 
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, true) 
			};

			var playlsitsTab = new PlaylistTabViewModelBase(m, "Top playlists", LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			
			Tabs.Add(playlsitsTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				await Initial_Update_Playlists();
				Initial_Setup();
				return true;
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			await Initial_Update_Playlists();
			Initial_Setup();
		}

		#endregion AuthMethods

		#region TransferMethods

		public override async Task Transfer_SaveInTo(object[] items)
		{
			await IsServiceSelectedAsync();
		}

		#endregion TransferMethods
	}
}