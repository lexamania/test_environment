using MusConv.MessageBoxManager.Texts;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class ProToolsViewModel : TransferOnlyViewModel
	{
		#region Constructors

        public ProToolsViewModel(MainViewModelBase m) : base(m, MessageBoxText.ProToolsHelp)
        {
            Title = "Pro Tools";
            SourceType = DataSource.ProTools;
            LogoKey = LogoStyleKey.ProToolsLogo;
            SideLogoKey = LeftSideBarLogoKey.ProToolsSideLogo;
        }

		#endregion Constructors
	}
}