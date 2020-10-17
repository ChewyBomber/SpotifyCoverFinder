using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SpotifyCoverFinder
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            token = GetToken();
        }

        private string GetToken()
        {
            using (HttpClient client = new HttpClient())
            {
                var values = new Dictionary<string, string> { { "grant_type", "client_credentials" } };
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", ConfigurationManager.AppSettings["apiToken"]);

                var content = new FormUrlEncodedContent(values);
                var response = client.PostAsync("https://accounts.spotify.com/api/token", content).Result;

                var responseString = response.Content.ReadAsStringAsync().Result;

                var parsedJson = JObject.Parse(responseString);

                return (string)parsedJson["access_token"];
            }
        }
        private string token = null;
        private JEnumerable<JToken> results;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            imagesList.Items.Clear();
            // Spotify
            Uri searchURI = new Uri("https://api.spotify.com/v1/search?type=album&market=FR&limit=50&q=" + query.Text);
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("Accept: application/json");
                client.Headers.Add("Authorization: Bearer " + token);
                JObject o = JObject.Parse(client.DownloadString(searchURI));
                results = o["albums"]["items"].Children();
                foreach (JToken result in results)
                {
                    DownloadCover(result, (string)result["images"].Last["url"]);
                }
            }
        }

        private async void DownloadCover(JToken obj, string imageURL)
        {
            BitmapImage imageData = await Task.Run(delegate ()
            {
                BitmapImage image = ByteToBitmap(new WebClient().DownloadData(new Uri(imageURL)));
                image.Freeze();
                return image;
            });
            Image newImage = new Image();
            newImage.Tag = obj;
            newImage.MouseLeftButtonDown += NewImage_MouseLeftButtonDown;
            newImage.MouseRightButtonDown += NewImage_MouseRightButtonDown;
            newImage.Width = 100;
            newImage.Height = 100;
            newImage.Source = imageData;
            imagesList.Items.Add(newImage);
        }

        private void NewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            JToken token = (JToken)((Image)sender).Tag;
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = (string)token["id"] + ".jpg",
                Filter = "Image (*.jpg)|*.jpg"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile((string)token["images"].First["url"], saveFileDialog.FileName);
                }
            }
        }

        private void NewImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            JToken token = (JToken)((Image)sender).Tag;
            Clipboard.SetImage(ByteToBitmap(new WebClient().DownloadData((string)token["images"].First["url"])));
        }

        private static BitmapImage ByteToBitmap(byte[] array)
        {
            using (var ms = new System.IO.MemoryStream(array))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }

        private void query_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Button_Click(this, new RoutedEventArgs());
        }
    }
}
