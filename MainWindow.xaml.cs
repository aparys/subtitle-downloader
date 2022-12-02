using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
namespace Subtitle_Downloader
{
    /// <summary>
    /// Collects a list of movie titles from IMDB.com.
    /// Collects subtitles of each movie from subscene.com.
    /// Collects the genres of each movie on IMDB.
    /// Saves the subtitles as a .txt file.
    /// title, year, and genres are in the name of the txt file.
    /// </summary>
    public partial class MainWindow : Window
    {
        private WebClient webClient;
        public MainWindow()
        {
            webClient = new WebClient();
            webClient.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:47.0) Gecko/20100101 Firefox/47.0";
            InitializeComponent();
            findTitlesBtn.Click += new RoutedEventHandler(findTitlesAsync);
            downloadBtn.Click += new RoutedEventHandler(downloadSRTClick);
        }

        private void downloadSRTClick(object sender, RoutedEventArgs e)
        {
            string IMDBGenres;
            string year, url, title, temp, href, imdbLink,http = @"https://subscene.com";
            string[] genres = { "Comedy", "Romance", "Short", "Crime", "Family", "Mystery", "Thriller", "Action", "Fantasy", "Adventure", "Sci-Fi", "Animation", "History", "Horror", "Music", "War", "Reality-Tv", "Biography", "Talk-Show", "Documentary", "Musical", "Sport", "Western ", "Game-Show", "News" };
            StreamWriter writer = new StreamWriter("Skipped titles.txt");
            HtmlWeb web = new HtmlWeb();
            HashSet<string> usedTitles = new HashSet<string>();

            foreach (var genre in genres)
            {
                try
                {
                    StreamReader fileReader = new StreamReader(genre + "-Titles.txt");
                    label.Content = "running";
                    while (!fileReader.EndOfStream)
                    {
                        Thread.Sleep(2000);
                        webClient = new WebClient();
                        webClient.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:47.0) Gecko/20100101 Firefox/47.0";
                        title = "";
                        temp = fileReader.ReadLine();
                        foreach (char c in temp)
                        {
                            if (c != ':')
                                title += c;
                        }
                        if (!usedTitles.Contains(title))//checks if title was already downloaded
                        {
                            label.Content = genre + " " + title;
                            UpdateLayout();
                            usedTitles.Add(title);
                            url = http + "/subtitles/" + title + "/english";
                            var htmlDoc = web.Load(url);
                            try
                            {
                                href = htmlDoc.DocumentNode.SelectSingleNode("//body/div[1]/div[2]/div[4]/table/tbody/tr[1]/td[1]/a").GetAttributeValue("href", null);
                                if (href != null)
                                {
                                    htmlDoc = web.Load(http + href);
                                    imdbLink = htmlDoc.DocumentNode.SelectSingleNode("/html/body/div/div[2]/div[2]/div[2]/div[2]/h1/a").GetAttributeValue("href", null);
                                    if (imdbLink != null)
                                    {
                                        url = http + htmlDoc.DocumentNode.SelectSingleNode("/html/body/div/div[2]/div[2]/div[2]/div[2]/ul/li[4]/div[1]/a").GetAttributeValue("href", null);
                                        htmlDoc = web.Load(imdbLink);
                                        var nodes = htmlDoc.DocumentNode.SelectNodes("/html/body/div[2]/main/div/section[1]/section/div[3]/section/section/div[3]/div[2]/div[1]/div[1]/div/a/span");
                                        IMDBGenres = "";
                                        foreach (var node in nodes)
                                        {
                                            IMDBGenres += "." + node.InnerText;
                                        }
                                        year = htmlDoc.DocumentNode.SelectSingleNode("/html/body/div[2]/main/div/section[1]/section/div[3]/section/section/div[1]/div[1]/div[2]/ul/li[1]/span").InnerText;
                                        //checks if file already exists
                                        if (!File.Exists("srt files/" + year + IMDBGenres + "." + title + ".zip"))
                                        {
                                            Task task = new Task(() => DownloadFile(url, "srt files/" + year + IMDBGenres + "." + title + ".zip", writer, title));
                                            task.Start();
                                        }
                                    }
                                }
                            }
                            catch { writer.WriteLineAsync(title); }
                        }
                    }
                }

                catch
                {//missing title file 
                }
            }
            label.Content = "Completed";
        }


        private async void findTitlesAsync()
        {

            HtmlWeb web = new HtmlWeb();
            HashSet<string> titles = new HashSet<string>();
            string[] genres = { "Comedy", "Romance", "Short", "Crime", "Family", "Mystery", "Thriller", "Action", "Fantasy", "Adventure", "Sci-Fi", "Animation", "History", "Horror", "Music", "War", "Reality-Tv", "Biography", "Talk-Show", "Documentary", "Musical", "Sport", "Western ", "Game-Show", "News" };
            progressbar.Value = 0;
            progressbar.Minimum = 0;
            progressbar.Maximum = 5000;
            int i = 0;
            string url;
            string title;
            StreamWriter file;
            foreach (var genre in genres)
            {
                label.Content = genre;
                file = new StreamWriter(genre + " Titles.txt", append: true);
                for (int page = 1; page < 201; page++)
                {
                    progressbar.Value = i * 200 + page;
                    label.Content += " page: " + page;
                    url = @"https://www.imdb.com/search/keyword/?pf_rd_m=A2FGELUUNOQJNL&pf_rd_p=a581b14c-5a82-4e29-9cf8-54f909ced9e1&pf_rd_r=403AXNA18ZJ90GEQ0RZG&pf_rd_s=center-5&pf_rd_t=15051&pf_rd_i=genre&ref_=kw_ref_gnr&mode=detail&page=" + page + "&genres=" + genre + "&sort=moviemeter,asc";
                    try
                    {
                        var htmlDoc = web.Load(url);
                        var node = htmlDoc.DocumentNode.SelectNodes("//body/div/div/div/div/div/div/div/div/div/div/h3/a");
                        foreach (var n in node.Nodes())
                        {
                            title = n.InnerText;
                            titles.Add(title);
                            await file.WriteLineAsync(title);
                            //*[@id="main"]/div/div[2]/div[1]/div/div[2]/a
                        }
                    }
                    catch (Exception d) { await file.WriteLineAsync("!!!ERROR " + d.Message); }

                }
                i++;
            }
            label.Content = "Title list completed";
            MessageBox.Show("Title list completed");
        }

        private async void findTitlesAsync(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => findTitlesAsync());
        }

        public void DownloadFile(string urlAddress, string location, StreamWriter writer, string title)
        {
            try
            {
                webClient.DownloadFileCompleted += (sender, EventArgs) =>
                {
                    string extractionPath = location.Substring(0, location.Length - 3) + "srt";
                    ZipFile.ExtractToDirectory(location, extractionPath);
                };
                Uri URL = urlAddress.StartsWith("https://subscene.com/", StringComparison.OrdinalIgnoreCase) ? new Uri(urlAddress) : new Uri("https://subscene.com/" + urlAddress);

                webClient.DownloadFile(URL, location);
                string extractionPath = location.Substring(0, location.Length - 3) + "srt";
                ZipFile.ExtractToDirectory(location, extractionPath);
            }
            catch { writer.WriteLine(title); }
        }
    }
}