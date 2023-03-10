using System;
using System.Linq;
using System.Threading.Tasks;
using MusConv.Lib.Kcrw;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Attributes;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.ViewModels.Base;
using static MusConv.MessageBoxManager.MessageBox;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    [WebUrlParser]
    public class KcrwViewModel : WebUrlViewModelBase
	{
		#region Constructors

        public KcrwViewModel(MainViewModelBase main) : base(main)
        {
            Title = "KCRW";
            SourceType = DataSource.Kcrw;
            LogoKey = LogoStyleKey.KcrwLogo;
            SideLogoKey = LeftSideBarLogoKey.KcrwSideLogo;
            Url = Urls.Kcrw;
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
                        var playlist = await KcrwParser.GetPlaylist(link).ConfigureAwait(false);

                        var musConvPlaylist = _mapper.PlaylistMapper.MapPlaylistWithTracks(playlist, playlist.Tracks);
                        SelectedTab.MediaItems.Add(musConvPlaylist);
                    }
                    catch
                    {
                        WrongURLs.Add(link);
                    }
                }

                await ShowWrongURLs();
                Initial_Setup();
            }
            catch (Exception e)
            {
                MusConvLogger.LogFiles(e);
                await ShowError(Texts.EnterCorrectWebURLs);
                MainViewModel.NavigateTo(NavigationKeysChild.WebUrl);
            }
        }

		#endregion AuthMethods
	}
}