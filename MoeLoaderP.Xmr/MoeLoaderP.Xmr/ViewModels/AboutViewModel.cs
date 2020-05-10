using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MoeLoaderP.Xmr.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public AboutViewModel()
        {
            Title = "Leaful";
            OpenWebCommand = new Command(async () => await Browser.OpenAsync("http://leaful.com"));
        }

        public ICommand OpenWebCommand { get; }
    }
}