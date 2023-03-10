using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	public class SlackerViewModel:SectionViewModelBase
	{
		#region InnerMethods

		public SlackerViewModel(MainViewModelBase m) : base(m)
		{
			Title = "Slacker";
		 //   SourceType = DataSource.Slacker;
			LogoKey = LogoStyleKey.LiveOneLogo;
			CurrentVMType = VmType.Service;
			RegState = RegistrationState.Unlogged;
		}

		#endregion InnerMethods
	}
}