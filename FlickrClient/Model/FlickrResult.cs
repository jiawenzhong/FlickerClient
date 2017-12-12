using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FlickrClient.Model
{
    public class FlickrResult: ObservableObject
    {
        /// <summary>
        /// title of the flickr result
        /// </summary>
        private string _title;
        public string Title
        {
            get
            {
                return _title;
            }

            set
            {
                Set<string>(() => this.Title, ref _title, value);
            }
        }

        /// <summary>
        /// the url of the flicr result
        /// </summary>
        private string _url;
        public string URL
        {
            get
            {
                return _url;
            }
            set
            {
                Set<string>(() => this.URL, ref _url, value);
            }
        }

    }
}
