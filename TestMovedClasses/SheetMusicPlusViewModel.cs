using Avalonia.Threading;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    [WebUrlParser]
    public class SheetMusicPlusViewModel : WebUrlViewModelBase
	{
		#region Constructors

        public SheetMusicPlusViewModel(MainViewModelBase m) : base(m)
        {
            Model = new SheetMusicPlusModel();
            Title = "Sheet Music Plus";
            SourceType = DataSource.SheetMusicPlus;
            LogoKey = LogoStyleKey.SheetMusicPlus;
            SideLogoKey = LeftSideBarLogoKey.SheetMusicPlusSideLogo;
            Url = "https://www.sheetmusicplus.com/";
        }

		#endregion Constructors

		#region AuthMethods

        public override async Task Web_NavigatingAsync(object s, object t)
        {
            try
            {
                SelectedTab.Loading = true;
                MainViewModel.NavigateTo(NavigationKeysChild.Content);
                SelectedTab.MediaItems.Clear();

                URLs = s.ToString().Split(Environment.NewLine).Where(x => !string.IsNullOrEmpty(x));
                foreach (var link in URLs)
                {
                    try
                    {
                        SelectedTab.MediaItems.AddRange(await (Model as SheetMusicPlusModel).GetPlaylistAsync(link).ConfigureAwait(false));
                    }
                    catch
                    {
                        WrongURLs.Add(link);
                        continue;
                    }
                }

                await ShowWrongURLs();
                Initial_Setup();
            }
            catch (Exception ex)
            {
                MusConvLogger.LogFiles(ex);
                await ShowError(Texts.EnterCorrectWebURLs);
                MainViewModel.NavigateTo(NavigationKeysChild.WebUrl);
            }
        }

		#endregion AuthMethods
	}
}