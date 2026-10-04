using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Windows;
// Asegúrate de tener este using:
using Microsoft.Web.WebView2.Wpf;

namespace RaupCoilerII {

    public partial class AboutWindow : Window {

        private string strVersion = "xx.xx.xx";
        private string strRemoteNewsWebsite = "";
        private string strLocalNewsWebsite = "";
        private string strRemoteMainWebsite = "";

        private WebView2 aboutWebBrowser;

        public AboutWindow() {
            InitializeComponent();
        }

        /*******************************************************************************
        * @brief auxiliar methro that checks if the web page specified in the received URL
        * does exist or does not exist.
        * @param[in] strURL string with the URL of the page to check if exists or not 
        * exists.
        * @return true if exists the web page withe received URL, false if it does not
        * exist.
        *******************************************************************************/
        static public bool CheckWebPageExists(string strURL) {
            bool bPageExists = false;

            // this code checks if the received URL exists or does not exist
            try {

                var request = WebRequest.Create(strURL) as HttpWebRequest;
                request.Method = "HEAD";
                using (var response = (HttpWebResponse)request.GetResponse()) {
                    bPageExists = response.StatusCode == HttpStatusCode.OK;
                }//using
            } catch {
                bPageExists = false;
            }//try

            return bPageExists;

        }//CheckWebPageExists

        /*******************************************************************************
        * @brief delegate that creates and initialzies some of the About window controls
        * when the window is created.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private async void Window_Loaded(object sender, RoutedEventArgs e) {
            // string strCompTime = Properties.Resources.BuildDate;
            string strCurrDir = "";
            string strBuildDate = "";
            string strVersion = "";

            strVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            strBuildDate = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(x => x.Key == "BuildDate")?.Value ?? "";

            // this.Text = String.Format("About {0} ", AssemblyTitle);
            this.lblProductName.Text = "RaupCoilerII";
            this.lblVersion.Text = "Version " + strVersion;
            this.lblBuild.Text = "Build date " + strBuildDate;

            // initialize the URL of the remote webpage to show in the AboutDialog box web browser
            strRemoteNewsWebsite = "https://www.tolaemon.com/raupcoilerii/news.php";
            strRemoteNewsWebsite = strRemoteNewsWebsite.ToLower();

            // initialize the URL of the local webpage to show in the AboutDialog box web browser in
            // case that the remote webpage is not avialable.
            strCurrDir = Directory.GetCurrentDirectory();
            strLocalNewsWebsite = "file:///" + strCurrDir + "/local.htm";
            strLocalNewsWebsite = strLocalNewsWebsite.Replace("\\", "/");
            strLocalNewsWebsite = strLocalNewsWebsite.ToLower();

            // 1. Instanciamos el WebView2 en tiempo de ejecución
            aboutWebBrowser = new WebView2();
            aboutWebBrowser.NavigationStarting += WebView_NavigationStarting; // configure our own NavigationStarting event

            // 2. Lo añadimos al Grid contenedor que dejamos en el XAML
            webContainer.Children.Add(aboutWebBrowser);

            // 3. Inicializamos el entorno WebView2
            await aboutWebBrowser.EnsureCoreWebView2Async();

            // Opcional: Evento de navegación
            // aboutWebBrowser.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

            // open the remote webpage or the local webpage deppending on if the remote website is available or not
            // to avoid that calling the CheckWebPageExists() function generates an entry in the remote .txt file
            if (CheckWebPageExists(strRemoteNewsWebsite)) {
                aboutWebBrowser.CoreWebView2.Navigate(strRemoteNewsWebsite);
            } else {
                aboutWebBrowser.CoreWebView2.Navigate(strLocalNewsWebsite);
            }

        }//Window_Loaded

        /*******************************************************************************
        * @brief delegate that captures the event when the user clicks on any link of the
        * web page in the WebContainer in order to open it in the system web browser, not
        * in thw WebContainer.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void WebView_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e) {
            string strPageName = "";
            Uri uri = null;

            // take the name of the open web page and if this web page is not one the 
            // application web pages we open it in the system web broswer and not 
            // in the RaupsCoiler about form web browser.
            uri = new Uri(e.Uri);
            strPageName = System.IO.Path.GetFileName(uri.AbsolutePath);
            if (!strPageName.Equals("local.htm", StringComparison.OrdinalIgnoreCase) &&
                !strPageName.Equals("news.php", StringComparison.OrdinalIgnoreCase)) {

                // canelate the navigation in the embedded web container
                e.Cancel = true;

                // open the clikcer URL in the default web browser
                Process.Start(new ProcessStartInfo {
                    FileName = e.Uri,
                    UseShellExecute = true
                });
            
            }//if

        }//WebView_NavigationStarting

        /*******************************************************************************
        * @brief delegate that captures the event when the user clicks on the License
        * label of the About form.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void LnkLblLicense_Click(object sender, RoutedEventArgs e) {

            try {

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = "https://tu-servidor.com/licencia",
                    UseShellExecute = true
                });

            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        
        }//LnkLblLicense_Click

        /*******************************************************************************
        * @brief delegate that captures the event when the user clicks on the Accept
        * label of the About form.
        * @param[in] sender reference to the object that raises the event
        * @param[in] e the information related to the event
        *******************************************************************************/
        private void BtnAccept_Click(object sender, RoutedEventArgs e) {

            this.Hide();
            this.Close();

        }//BtnAccept_Click

    }//AboutWindow : Window

}//namespace RaupCoilerII