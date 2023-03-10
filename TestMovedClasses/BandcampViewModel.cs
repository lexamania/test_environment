using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class BandcampViewModel : SectionViewModelBase
	{
		#region Constructors

		public BandcampViewModel(MainViewModelBase m): base(m)
		{
			Title = "Bandcamp";
			SourceType = DataSource.Bandcamp;
			LogoKey = LogoStyleKey.BandcampLogo;
			SmallLogoKey = LogoStyleKey.BandcampLogoSmall;
			SideLogoKey = LeftSideBarLogoKey.BandcampSideLogo;
			RegState = RegistrationState.Unlogged;
			CurrentVMType = VmType.Service;
			Model = new BandcampModel();
			Url = "https://bandcamp.com/login";
			BaseUrl = "https://bandcamp.com/";

			#region Commands

			var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase);

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase);

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			#region TransferTasks

			var albumTransfer = new List<TaskBase_TaskItem>
			{
				//new AlbumTransferTaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send)
			};
			var trackTransfer = new List<TaskBase_TaskItem>
			{
				//new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_Search, TransferTrack_Send)
			};
			var artistTransfer = new List<TaskBase_TaskItem>
			{ 
				new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
			};

			#endregion TransferTasks

			#region Tabs
			
			var wishlistAlbumTab = new AlbumTabViewModelBase(m, "Wishlist albums", LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_WishlistAlbums), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));
			var wishlistTracksTab = new TrackTabViewModelBase(m, "Wishlist tracks", LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_WishlistTracks), EmptyCommandsBase, 
				new LogOut_TaskItem("LogOut", Log_Out));
			var collectedAlbumsTab = new AlbumTabViewModelBase(m, "Collected albums", LwTabIconKey.AlbumIcon, 
				albumTransfer, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_CollectedAlbums), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));
			var collectedTracksTab = new TrackTabViewModelBase(m, "Collected tracks", LwTabIconKey.TrackIcon, 
				trackTransfer, commandTracksTab,
				new Initial_TaskItem("Reload", Initial_Update_CollectedTracks), EmptyCommandsBase, 
				new LogOut_TaskItem("LogOut", Log_Out));
			var artistTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistTransfer, commandArtistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks, 
				new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs
			
			Tabs.Add(wishlistAlbumTab);
			Tabs.Add(wishlistTracksTab);
			Tabs.Add(collectedAlbumsTab);
			Tabs.Add(collectedTracksTab);
			Tabs.Add(artistTab);
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (!Model.IsAuthorized)
				{
					if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data)&& await IsServiceDataExecuted(data))
					{
						return false;
					}
					await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
					return false;
				}
				else
				{
					await InitialUpdateForCurrentTab().ConfigureAwait(false);
					return true;
				}
			}
			catch (Exception ex)
			{
				MusConvLogger.LogFiles(ex);
				return false;
			}
		}

		public override async Task Web_NavigatingAsync(object s, object t)
		{
			var model = Model as BandcampModel;
			var userLink = (t as string) ?? Url;  
			model?.Initialize(s as string, userLink);
			Model.IsAuthorized = true;
			SaveLoadCreds.SaveData(new List<string>{GetSerializedServiceData(userLink, s as string)});
			await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
			RegState = RegistrationState.Logged;
			if (IsSending)
			{
				WaitAuthentication.Set();
				IsSending = false;
			}
			else
			{
				await InitialUpdateForCurrentTab(true).ConfigureAwait(false);
			}
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			Url = "https://bandcamp.com/login";
			ClearAllMediaItems();
			await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var bandcampData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			await Web_NavigatingAsync(bandcampData["Identity"], bandcampData["UserLink"]);
			return true;
		}

		#endregion AuthMethods

		#region InitialUpdate

		public Task<bool> Initial_Update_CollectedAlbums(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as BandcampModel).GetCollectedAlbums, SelectedTab, forceUpdate);
		}

		public Task<bool> Initial_Update_CollectedTracks(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as BandcampModel).GetCollectedTracks, SelectedTab, forceUpdate);
		}

		public Task<bool> Initial_Update_WishlistAlbums(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as BandcampModel).GetWishlistAlbums, SelectedTab, forceUpdate);
		}

		public Task<bool> Initial_Update_WishlistTracks(bool forceUpdate = false)
		{
			return InitialUpdateBuilder((Model as BandcampModel).GetWishlistTracks, SelectedTab, forceUpdate);
		}

		#endregion InitialUpdate

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			try
			{
				WaitAuthentication = new AsyncAutoResetEvent();

				if (!Model.IsAuthorized)
				{
					await IsServiceSelectedAsync().ConfigureAwait(false);
				}
				else
				{
					WaitAuthentication.Set();
				}
			}
			catch (Exception e)
			{
				MusConvLogger.LogFiles(e);
			}

			await Transfer_DoWork(items[0]);
		}

		#endregion TransferMethods

		#region InnerMethods

		private string GetSerializedServiceData(string userLink, string identity)
		{
			return Serializer.Serialize(new Dictionary<string, string>
			{
				{ "UserLink", userLink},
				{ "Identity", identity}
			});
		}

		#endregion InnerMethods
	}
}