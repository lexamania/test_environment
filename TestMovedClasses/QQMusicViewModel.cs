using Avalonia.Threading;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models.MusicService;
using MusConv.Sentry;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusConv.Abstractions;
using System.Net;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class QQMusicViewModel: SectionViewModelBase
	{
		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			try
			{
				MainViewModel.NeedLogin = this;
				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

				if (true)
				{
					if (IsDeveloperLicense && SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) && await IsServiceDataExecuted(data))
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
			if (Model.IsAuthenticated() == false)
			{

				await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				await Model.Initialize(s).ConfigureAwait(false);
				if (IsDeveloperLicense)
				{
					SaveLoadCreds.SaveData(new List<string>{GetSerializedServiceData(s, t)});
				}
			}

			RegState = RegistrationState.Logged;
			if (IsSending)
			{
				WaitAuthentication.Set();
				IsSending = false;
			}
			else
			{
				await Initial_Update_Playlists().ConfigureAwait(false);

			}
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			RegState = RegistrationState.Unlogged;
			Model.IsAuthorized = false;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				foreach (var item in Tabs)
				{
					item.MediaItems.Clear();
				}
				NavigateToBrowserLoginPage();
			});

			return true;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var amazonData = Serializer.Deserialize<Dictionary<string, string>>(data[Title].FirstOrDefault());
			var dict = Serializer.Deserialize<Dictionary<string, object>>(amazonData["Data"]);
			var cookie = Serializer.Deserialize<List<Cookie>>(amazonData["Cookie"])
				.Select(x => new Cookie() { Name = x.Name, Value = x.Value, Domain = x.Domain, Path = x.Path, }).ToList();
			await Web_NavigatingAsync(dict, cookie);

			return true;
		}

		#endregion AuthMethods

		#region InnerMethods

		public QQMusicViewModel(MainViewModelBase m): base(m)
		{
			Title = "QQ Music";
			CurrentVMType = VmType.Service;
			RegState = RegistrationState.Unlogged;
			SourceType = DataSource.QQMusic;
			LogoKey = LogoStyleKey.QQMusicLogo;
			SideLogoKey = LeftSideBarLogoKey.QQMusicSideLogo;
			Model = new QQMusicModel();
			Url = "https://y.qq.com/";
			BaseUrl = "https://y.qq.com/";

			#region Commands

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
			};

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

			var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase);

			#endregion Commands

			var playlsitsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				EmptyTransfersBase, commandPlaylistsTab, 
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));
			var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				EmptyTransfersBase, commandAlbumsTab,
				new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
				new LogOut_TaskItem("LogOut", Log_Out));

			Tabs.Add(playlsitsTab);
			//Tabs.Add(albumsTab);
		}

		private string GetSerializedServiceData(object headers, object cookies)
		{
			return Serializer.Serialize(new Dictionary<string, string>
								{
									{ "Data", Serializer.Serialize(headers as Dictionary<string, object>)},
									{ "Cookie", Serializer.Serialize(cookies as List<Cookie>)},
								});
		}

		#endregion InnerMethods
	}
}