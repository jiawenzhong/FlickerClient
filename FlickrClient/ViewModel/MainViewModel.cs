using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using System.Net;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.ComponentModel;
using System.Data;
using FlickrClient.Model;

namespace FlickrClient.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        // Use your Flickr API key here--you can get one at:
        // http://www.flickr.com/services/apps/create/apply
        private const string KEY = "d12c1d908607a89aa8278ca0ffae4d89";

        /// <summary>
        /// object used to invoke Flickr web service
        /// </summary>
        private WebClient flickrClient = new WebClient();

        /// <summary>
        /// the task that request from server
        /// </summary>
        Task<string> flickrTask = null; // Task<string> that queries Flickr

        /// <summary>
        /// search button command property
        /// </summary>
        public ICommand SearchCommand { get; private set; }

        /// <summary>
        /// display image command property
        /// </summary>
        public ICommand DisplayCommand { get; private set; }

        /// <summary>
        /// exit button command property
        /// </summary>
        public ICommand ExitCommand { get; private set; }

        /// <summary>
        /// store the image's bitmap sourse from download
        /// </summary>
        private ImageSource _photo;
        public ImageSource Photo
        {
            get
            {
                return this._photo;
            }
            set
            {
                this._photo = value;
                this.RaisePropertyChanged("Photo");
            }
        }

        /// <summary>
        /// store the input from the search text box
        /// </summary>
        private string _search;
        public string Search
        {
            get
            {
                return this._search;
            }

            set
            {
                this._search = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// the selected element in the observable FlickrResult list
        /// </summary>
        private FlickrResult _selectedSearch;
        public FlickrResult SelectedSearch
        {
            get
            {
                return this._selectedSearch;
            }

            set
            {
                this._selectedSearch = value;
                this.RaisePropertyChanged("SelectedSearch");
            }
        }

        /// <summary>
        /// list that sort the FlickrResults
        /// </summary>
        private ObservableCollection<FlickrResult> _searchList = new ObservableCollection<FlickrResult>();
        public ObservableCollection<FlickrResult> SearchList
        {
            get
            {
                return _searchList;
            }
        }


        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            //commands on each button action
            SearchCommand = new RelayCommand(OnSearchCommandAsync);
            DisplayCommand = new RelayCommand(OnDisplayCommand);
            ExitCommand = new RelayCommand<Window>(CloseWindow);
        }

        public async void OnSearchCommandAsync()
        {
            // if flickrTask already running, prompt user 
            if (flickrTask != null &&
               flickrTask.Status != TaskStatus.RanToCompletion)
            {
                var result = MessageBox.Show(
                   "Cancel the current Flickr search?",
                   "Sure?", MessageBoxButton.YesNo,
                   MessageBoxImage.Question);

                // determine whether user wants to cancel prior search
                if (result == MessageBoxResult.No)
                    return;
                else
                    flickrClient.CancelAsync(); // cancel current search
            } // end if

            // Flickr's web service URL for searches
            var flickrURL = string.Format("https://api.flickr.com/services" +
               "/rest/?method=flickr.photos.search&api_key={0}&tags={1}" +
               "&tag_mode=all&per_page=500&privacy_filter=1", KEY,
               Search.Replace(" ", ","));

            //DataSource = null; // remove prior data source
            SearchList.Clear(); // clear imagesListBox
            Photo = null; // clear pictureBox
            FlickrResult loading = new FlickrResult()
            {
                Title = "Loading..."
            };
            SearchList.Add(loading); // display Loading...

            try
            {
                // invoke Flickr web service to search Flick with user's tags
                flickrTask =
                   flickrClient.DownloadStringTaskAsync(flickrURL);

                // await flickrTask then parse results with XDocument and LINQ
                XDocument flickrXML = XDocument.Parse(await flickrTask);

                // gather information on all photos
                var flickrPhotos =
                   (from photo in flickrXML.Descendants("photo")
                   let id = photo.Attribute("id").Value
                   let title = photo.Attribute("title").Value
                   let secret = photo.Attribute("secret").Value
                   let server = photo.Attribute("server").Value
                   let farm = photo.Attribute("farm").Value
                   select new FlickrResult
                   {
                       Title = title,
                       URL = string.Format(
                         "http://farm{0}.staticflickr.com/{1}/{2}_{3}.jpg",
                         farm, server, id, secret)
                   }).ToList();

                _searchList.Clear(); // clear imagesListBox

                //add result into the list
                foreach(FlickrResult fr in flickrPhotos)
                    _searchList.Add(fr);

                // if there were no matching results
                if (_searchList.Count == 0)
                {
                    FlickrResult noMatch = new FlickrResult()
                    {
                        Title = "No matching"
                    };
                    _searchList.Add(noMatch);
                }
            } // end try
            catch (WebException)
            {
                // check whether Task failed
                if (flickrTask.Status == TaskStatus.Faulted)
                    MessageBox.Show("Unable to get results from Flickr",
                       "Flickr Error", MessageBoxButton.OK,
                       MessageBoxImage.Error);
                SearchList.Clear(); // clear imagesListBox
                FlickrResult error = new FlickrResult()
                {
                    Title = "Error matching"
                };
                _searchList.Add(error);
            } // end catch
        }

        /// <summary>
        /// display the photo of selected FlickrResult into the image in view
        /// </summary>
        public async void OnDisplayCommand()
        {
            if (SelectedSearch != null)
            {
                string selectedURL =
                   ((FlickrResult)SelectedSearch).URL;

                // use WebClient to get selected image's bytes asynchronously
                WebClient imageClient = new WebClient();
                byte[] imageBytes = await imageClient.DownloadDataTaskAsync(
                   selectedURL);

                // display downloaded image in pictureBox
                MemoryStream memoryStream = new MemoryStream(imageBytes);
                //turn memorystream into bitmap so the windows control image can support it
                var bitmap = new BitmapImage();
                bitmap.BeginInit();//start bitmap initialization
                bitmap.StreamSource = memoryStream;//set the source of the bitmap
                bitmap.CacheOption = BitmapCacheOption.OnLoad;//cache image into memory when its loaded
                bitmap.EndInit();//finish initialization
                bitmap.Freeze();//make object unmodifiable
                //set the bitmap into photo
                Photo = bitmap;
            }
        }

        /// <summary>
        /// close the window
        /// </summary>
        /// <param name="window"></param>
        private void CloseWindow(Window window)
        {
            if (window != null)
            {
                window.Close();
            }
        }
    }
}