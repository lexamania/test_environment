using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.ViewModels.Base;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
	class MovistarMusicaViewModel: EmptyViewModel
	{
		#region InnerMethods

		public MovistarMusicaViewModel(MainViewModelBase m): base(m)
		{
			Title = "Movistar Musica";
			SourceType = DataSource.MovistarMusica;
			LogoKey = LogoStyleKey.MovistarMusicaLogo;
			SideLogoKey = LeftSideBarLogoKey.MovistarMusicaSideLogo;
			BaseUrl = "https://www.movistar.co/movistar-musica";
		}

		#endregion InnerMethods
	}
}