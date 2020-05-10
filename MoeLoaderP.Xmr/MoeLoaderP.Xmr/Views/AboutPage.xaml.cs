using System;
using System.ComponentModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MoeLoaderP.Xmr.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
            GoHomeButton.Clicked += GoHomeButtonOnClicked;
        }

        private async void GoHomeButtonOnClicked(object sender, EventArgs e)
        {
            await Browser.OpenAsync("http://leaful.com");
        }
    }
}