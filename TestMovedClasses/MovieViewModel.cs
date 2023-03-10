using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusConv.Lib.TuneFind.Exceptions;
using MusConv.Navigation.Keys.NavigationKeys;
using MusConv.Sentry;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using static MusConv.MessageBoxManager.MessageBox;
using static System.Environment;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class MovieViewModel : SectionViewModelBase
	{
		#region Fields

        private MovieModel MovieModel { get; set; }

		#endregion Fields

		#region Constructors

        public MovieViewModel(MainViewModelBase m) : base(m)
        {
            Title = "Movie Soundtracks";
            SourceType = DataSource.Movie;
            LogoKey = LogoStyleKey.MovieLogo;
            SideLogoKey = LeftSideBarLogoKey.MovieSideLogo;
            RegState = RegistrationState.Needless;
            IsTransferAvailable = false;
            MovieModel = new MovieModel();
            Model = MovieModel;

			#region Commands

            var commandPlaylistTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase);

            var commandTracks = new List<Command_TaskItem> (TracksCommandsBase);

			#endregion Commands

            var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon,
                EmptyTransfersBase, commandPlaylistTab, 
				new Initial_TaskItem("Reload", () => { }), commandTracks);

            Tabs.Add(playlistsTab);
        }

		#endregion Constructors

		#region DataProperties

        private List<string> _notFoundFilms = new();
        private MovieModel MovieModel { get; set; }

		#endregion DataProperties

		#region AccountsProperties

        private List<string> _notFoundFilms = new();
        private MovieModel MovieModel { get; set; }

		#endregion AccountsProperties

		#region MainProperties

        private List<string> _notFoundFilms = new();
        private MovieModel MovieModel { get; set; }

		#endregion MainProperties

		#region ItemsProperties

        private List<string> _notFoundFilms = new();
        private MovieModel MovieModel { get; set; }

		#endregion ItemsProperties

		#region AuthMethods

        public override async Task Web_NavigatingAsync(object s, object t)
        {
            SelectedTab.Loading = true;
            MainViewModel.NavigateTo(NavigationKeysChild.Content);
            SelectedTab.MediaItems.Clear();

            try
            {
                var films = s.ToString().Split(Environment.NewLine).Where(x => !string.IsNullOrEmpty(x));

                foreach (var film in films)
                {
                    MusConvPlayList playlist;
                    try
                    {
                        playlist = await MovieModel.GetMoviePlaylist(film).ConfigureAwait(false);
                        SelectedTab.MediaItems.Add(playlist);
                    }
                    catch (MovieNotFoundException ex)
                    {
                        _notFoundFilms.Add(film);
                        MusConvLogger.LogFiles(ex);
                    }
                    catch (Exception ex)
                    {
                        MusConvLogger.LogFiles(ex);
                    }
                }

                if (_notFoundFilms.Count != 0)
                    ShowMessage($"Wrong movie names: {NewLine} {String.Join(NewLine, _notFoundFilms)}");

                _notFoundFilms.Clear();

                if (!SelectedTab.MediaItems.Any())
                    MainViewModel.NavigateTo(NavigationKeysChild.MoviesInputPage);
            }
            catch (Exception ex)
            {
                MusConvLogger.LogFiles(ex);
                ShowError(Texts.EnterCorrectMovies);
                MainViewModel.NavigateTo(NavigationKeysChild.MoviesInputPage);
            }
            SelectedTab.Loading = false;
        }

        public override void SelectService()
        {
            MainViewModel.NeedLogin = this;
            MainViewModel.NavigateTo(NavigationKeysChild.MoviesInputPage);
        }

		#endregion AuthMethods
	}
}