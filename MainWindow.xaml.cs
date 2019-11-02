using HtmlGrabber.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Linq;
using System.Globalization;
using System.Windows.Data;
using DataTableExport;
using Microsoft.Win32;

namespace HtmlGrabber
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string WebsiteJsonFile = AppDomain.CurrentDomain.BaseDirectory + "ConfigFiles\\Websites.json";
        private static DataTable dtLiveHistory = new DataTable();
        private static DataTable dtWebsiteList = new DataTable();

        public MainWindow()
        {
            InitializeComponent();

            dtWebsiteList.Columns.Add("Remark", typeof(string));
            dtWebsiteList.Columns.Add("Live Viewers", typeof(int));
            dtWebsiteList.Columns.Add("Max Viwers", typeof(int));
            dtWebsiteList.Columns.Add("Refresh Time", typeof(int));
            dtWebsiteList.Columns.Add("Find Regex", typeof(string));
            dtWebsiteList.Columns.Add("Match", typeof(int));
            dtWebsiteList.Columns.Add("Website Link", typeof(string));


            dtLiveHistory.Columns.Add("Remark", typeof(string));
            dtLiveHistory.Columns.Add("Live Viewers", typeof(int));
            dtLiveHistory.Columns.Add("Date_Time", typeof(DateTime));

            DataGridHistory.ItemsSource = dtLiveHistory.DefaultView;
            dataGridWebsiteList.ItemsSource = dtWebsiteList.DefaultView;


        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            //string FindRegex = "(\"viewCount\": \{ \"runs\": \[\{ \"text\": \")(\d+.*)( watching now\" \}\] \}, \"isLive\": true)";
            try
            {
                string WebsiteLink = txtWebsiteLink.Text.Trim();
                string FindRegex = txtFindRegex.Text.Trim();
                string Match = txtMatch.Text.Trim();
                string RefreshTime = txtRefreshTime.Text.Trim();
                string Remark = txtRemark.Text.Trim();

                if (dtWebsiteList.Rows.Count > 0)
                {
                    int duplicateCount = dtWebsiteList.AsEnumerable().Where(x => x.Field<string>("Remark").ToLower() == Remark.ToLower()).Count();
                    if (duplicateCount > 0)
                    {
                        MessageBox.Show("Remarks can't be duplicate.", "Duplication found", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                DataRow dataRow = dtWebsiteList.NewRow();
                dataRow["Remark"] = Remark;
                dataRow["Live Viewers"] = 0;
                dataRow["Refresh Time"] = Convert.ToInt32(RefreshTime);
                dataRow["Find Regex"] = FindRegex;
                dataRow["Match"] = Convert.ToInt32(Match);
                dataRow["Max Viwers"] = 0;
                dataRow["Website Link"] = WebsiteLink;
                dtWebsiteList.Rows.Add(dataRow);

                Thread HtmlGrabberThread = new Thread(() => ProcessWebsiteData(WebsiteLink, FindRegex, Match, RefreshTime, Remark));
                HtmlGrabberThread.Start();

            }
            catch
            {

            }


        }

        private void ProcessWebsiteData(string websiteLink, string findRegex, string _match, string refreshTime, string remark)
        {
            while (true)
            {
                try
                {
                    DateTime startTime = DateTime.Now;
                    string htmlCode = string.Empty;
                    using (WebClient client = new WebClient())
                    {
                        htmlCode = client.DownloadString(websiteLink);
                    }


                    if (!string.IsNullOrEmpty(htmlCode))
                    {
                        if (Regex.IsMatch(htmlCode, findRegex, RegexOptions.Multiline))
                        {
                            Match match = Regex.Match(htmlCode, findRegex, RegexOptions.Multiline);
                            int found_match = -10;
                            int.TryParse(_match, out found_match);
                            if (found_match != -10)
                            {
                                string strLiveViewers = match.Groups[found_match].Value.ToString().Replace(",", "");
                                //MessageBox.Show("Live Viewers Count  : " + LiveViewers);
                                int LiveViewers = Convert.ToInt32(strLiveViewers);
                                DataRow dataRow = dtLiveHistory.NewRow();
                                dataRow["Remark"] = remark;
                                //var culture = CultureInfo.GetCultureInfo("en-US");
                                dataRow["Live Viewers"] = LiveViewers;
                                dataRow["Date_Time"] = DateTime.Now;
                                dtLiveHistory.Rows.Add(dataRow);


                                //Dispatcher.Invoke(() =>
                                //{
                                //DataGridHistory.ItemsSource = dtLiveHistory.DefaultView;

                                //});
                                Dispatcher.BeginInvoke(new ThreadStart(() => RefreshDataGridHistory()));
                                Dispatcher.BeginInvoke(new ThreadStart(() => RefreshDataGridWebsiteList(remark, LiveViewers)));
                                Dispatcher.BeginInvoke(new ThreadStart(() => RefreshTotalViewers()));



                            }
                        }
                    }
                    DateTime endTime = DateTime.Now;
                    int TimeRemain = Convert.ToInt32((endTime - startTime).TotalSeconds) - Convert.ToInt32(refreshTime);
                    if (TimeRemain < 0)
                    {
                        Thread.Sleep(TimeRemain * -1000);
                    }

                }
                catch
                {

                }

            }
        }

        //https://www.aspsnippets.com/Articles/Calculate-Sum-Total-of-DataTable-Columns-using-C-and-VBNet.aspx
        private void RefreshTotalViewers()
        {
            int TotalViewers = dtWebsiteList.AsEnumerable().Sum(row => row.Field<int>("Max Viwers"));
            LbTotalViewers.Content = TotalViewers.ToString();
        }

        //https://www.codeproject.com/Questions/362011/Find-and-Update-cell-in-DataTable
        private void RefreshDataGridWebsiteList(string remark, int LiveViewers)
        {
            try
            {
                int maxViewersFound = dtWebsiteList.AsEnumerable().Where(x => x.Field<string>("Remark").ToLower() == remark.ToLower() &&
                          x.Field<int>("Max Viwers") < LiveViewers).Count();

                //DataRow dr = dtWebsiteList.Select("Remark='" + remark + "'").Max();
                if (maxViewersFound > 0)
                {

                    // Get all DataRows where the name is the name you want.
                    IEnumerable<DataRow> rows = dtWebsiteList.Rows.Cast<DataRow>().Where(r => r["Remark"].ToString().ToLower() == remark.ToLower());
                    // Loop through the rows and change the name.
                    rows.ToList().ForEach(r => r.SetField("Max Viwers", LiveViewers));
                    rows.ToList().ForEach(r => r.SetField("Live Viewers", LiveViewers));
                    dataGridWebsiteList.Items.Refresh();

                }
                else
                {
                    // Get all DataRows where the name is the name you want.
                    IEnumerable<DataRow> rows = dtWebsiteList.Rows.Cast<DataRow>().Where(r => r["Remark"].ToString().ToLower() == remark.ToLower());
                    // Loop through the rows and change the name.                
                    rows.ToList().ForEach(r => r.SetField("Live Viewers", LiveViewers));
                    dataGridWebsiteList.Items.Refresh();
                }
            }
            catch
            {

            }


        }

        private void RefreshDataGridHistory()
        {
            try
            {
                DataGridHistory.Items.Refresh();
                DataGridHistory.ScrollIntoView(CollectionView.NewItemPlaceholder);
            }
            catch
            {

            }

        }

        private void HtmlGrabberSample()
        {
            string FindRegex = Properties.Resources.FindRegex;
            string ReplaceRegex = Properties.Resources.ReplaceRegex;
            string htmlCode = string.Empty;
            using (WebClient client = new WebClient())
            {
                htmlCode = client.DownloadString("https://www.youtube.com/watch?v=CZS9vk-Sa8A");
            }


            if (!string.IsNullOrEmpty(htmlCode))
            {
                if (Regex.IsMatch(htmlCode, FindRegex, RegexOptions.Multiline))
                {
                    Match match = Regex.Match(htmlCode, FindRegex, RegexOptions.Multiline);
                    string LiveViewers = match.Groups[2].Value.ToString();
                    MessageBox.Show("Live Viewers Count  : " + LiveViewers);
                }

            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //BindGridData();
        }

        private void BindGridData()
        {
            if (File.Exists(WebsiteJsonFile))
            {
                var jsonString = File.ReadAllText(WebsiteJsonFile);

                if (!string.IsNullOrEmpty(jsonString))
                {
                    List<WebsiteHelper> websiteHelpersList = JsonConvert.DeserializeObject<List<WebsiteHelper>>(jsonString);
                    DataTable dataTable = new DataTable();
                    Common common = new Common();
                    dataTable = common.ToDataTable(websiteHelpersList);
                    if (dataTable != null)
                    {
                        dataGridWebsiteList.DataContext = dataTable;
                    }
                }
            }

            //try
            //{
            //    var jObject = JObject.Parse(jsonString);

            //    if (jObject != null)
            //    {
            //        Console.WriteLine("ID :" + jObject["id"].ToString());
            //        Console.WriteLine("Name :" + jObject["name"].ToString());

            //        var address = jObject["address"];
            //        Console.WriteLine("Street :" + address["street"].ToString());
            //        Console.WriteLine("City :" + address["city"].ToString());
            //        Console.WriteLine("Zipcode :" + address["zipcode"]);
            //        JArray experiencesArrary = (JArray)jObject["experiences"];
            //        if (experiencesArrary != null)
            //        {
            //            foreach (var item in experiencesArrary)
            //            {
            //                Console.WriteLine("company Id :" + item["companyid"]);
            //                Console.WriteLine("company Name :" + item["companyname"].ToString());
            //            }

            //        }
            //        Console.WriteLine("Phone Number :" + jObject["phoneNumber"].ToString());
            //        Console.WriteLine("Role :" + jObject["role"].ToString());

            //    }
            //}
            //catch (Exception)
            //{

            //    throw;
            //}
        }

        private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            DataGridHistory.DataContext = null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (dtWebsiteList.Rows.Count > 0)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "CSV file (*.csv)|*.csv";
                if (saveFileDialog.ShowDialog() == true)
                {                    
                    dtWebsiteList.ToCSV(saveFileDialog.FileName);
                }                    
            }
           
        }
    }
}
