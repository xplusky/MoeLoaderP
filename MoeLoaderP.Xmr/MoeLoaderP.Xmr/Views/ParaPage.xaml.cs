using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoeLoaderP.Core;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MoeLoaderP.Xmr.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ParaPage : ContentPage
    {
        private Settings Settings { get; set; }
        private SiteManager SiteManager { get; set; }
        private SearchSession CurrentSearch { get; set; }

        public ParaPage()
        {
            InitializeComponent();
            Init();
        }

        public void Init()
        {
            SiteManager = new SiteManager(new Settings());
            Lv1SiteMenu.ItemsSource = SiteManager.Sites;
        }
    }

}