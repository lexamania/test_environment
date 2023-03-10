using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Lib.Spinrilla.Models;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class SpinrillaViewModel : EmptyViewModel
	{
		#region Constructors

		public SpinrillaViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Spinrilla";
			RegState = RegistrationState.Unlogged;
			Model = new SpinrillaModel();
			SourceType = DataSource.Spinrilla;
			LogoKey = LogoStyleKey.SpinrillaLogo;
			SideLogoKey = LeftSideBarLogoKey.SpinrillaSideLogo;
		}

		#endregion Constructors

		#region AuthMethods

		public override async Task<bool> IsServiceSelectedAsync()
		{
			await  Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
			return false;
		}

		public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
		{
			var serviceData = data.FirstOrDefault(x => x.Key == Title).Value;
			var credentials = Serializer.Deserialize<SpinrillaCreds>(serviceData.FirstOrDefault());

			if ((await Model.Initialize(credentials)).LoginNavigationState != LoginNavigationState.Done)
			{
				await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
				return false;
			}

			if (Accounts.Count == 0)
			{
				await LoadUserAccountInfo(serviceData).ConfigureAwait(false);
			}

			await InitialAuthorization();
			return true;
		}

		public override async Task LoadUserAccountInfo(List<string> data)
		{
			foreach (var accountData in data)
			{
				var credentials = Serializer.Deserialize<SpinrillaCreds>(accountData);

				var newModel = new SpinrillaModel();
				await newModel.Initialize(credentials);

				var accInfo = new AccountInfo()
				{
					Creds = Serializer.Serialize(credentials),
					Name = newModel.Email
				};

				Accounts.Add(newModel, accInfo);
			}
		}

		public override async Task<bool> Log_Out(bool forceUpdate = false)
		{
			SaveLoadCreds.DeleteServiceData();
			Accounts.Remove(Model);
			await Model.Logout().ConfigureAwait(false);
			RegState = RegistrationState.Unlogged;
			LogOutRequired = true;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				foreach (var item in Tabs)
					item.MediaItems.Clear();

				NavigateToMain();
			});

			return true;
		}

		#endregion AuthMethods

		#region TransferMethods

		public override async Task Transfer_SaveInTo(params object[] items)
		{
			MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
			IsSending = true;

			if (!Model.IsAuthenticated())
			{
				if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) || !await IsServiceDataExecuted(data))
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToEmailPasswordLoginForm);
				}
				else
				{
					await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
				}
			}
			else
			{
				WaitAuthentication.Set();
				IsSending = false;
			}

			await Transfer_DoWork(items[0]);
		}

		#endregion TransferMethods
	}
}