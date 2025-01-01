using DataTableExport;
using HtmlGrabber.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace HtmlGrabber
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string WebsiteJsonFile = AppDomain.CurrentDomain.BaseDirectory + "ConfigFiles\\Websites.json";
        private string LogPath = AppDomain.CurrentDomain.BaseDirectory + "Log";
        private static DataTable dtLiveHistory = new DataTable();
        private static DataTable dtWebsiteList = new DataTable();
        private static bool isWebsiteLinkCheckingTaskRunning = false;
        private static bool isRightWebsiteLink = false;
        private Dictionary<string, Thread> threadDictionary = new Dictionary<string, Thread>();
        private static int autoLogdeletedays = 7;
        private static bool isAutoLogdeleteEnable = true;
        private static bool isAutoLogSaveEnable = true;
        private ErrorHandle clsLogger = new ErrorHandle();

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                clsLogger.WriteToLogFile("Initialization of Main Window started.");
                dtWebsiteList.Columns.Add("Remark", typeof(string));
                dtWebsiteList.Columns.Add("Live Viewers", typeof(int));
                dtWebsiteList.Columns.Add("Max Viwers", typeof(int));
                dtWebsiteList.Columns.Add("Status", typeof(string));
                dtWebsiteList.Columns.Add("Refresh Time", typeof(int));
                dtWebsiteList.Columns.Add("Match", typeof(int));
                dtWebsiteList.Columns.Add("Retry Count", typeof(int));
                dtWebsiteList.Columns.Add("Website Link", typeof(string));
                dtWebsiteList.Columns.Add("Find Regex", typeof(string));
                dtWebsiteList.Columns.Add("TStatus", typeof(string));

                dtLiveHistory.Columns.Add("Remark", typeof(string));
                dtLiveHistory.Columns.Add("Live Viewers", typeof(int));
                dtLiveHistory.Columns.Add("Date_Time", typeof(DateTime));
                DataGridHistory.ItemsSource = dtLiveHistory.DefaultView;
                dataGridWebsiteList.ItemsSource = dtWebsiteList.DefaultView;

                Thread LogDeleteThread = new Thread(() => DeleteLog());
                LogDeleteThread.Name = "DeleteLog";
                LogDeleteThread.Start();

                Thread AutoLogSaveThread = new Thread(() => AutoLogSave());
                AutoLogSaveThread.Name = "AutoLogSave";
                AutoLogSaveThread.Start();
                clsLogger.WriteToLogFile("Initialization of Main Window Finished.");
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error in initialization of Main Window : " + ex.Message);
            }
        }

        private void AutoLogSave()
        {
            while (isAutoLogSaveEnable)
            {
                try
                {
                    clsLogger.WriteToLogFile("Autolog saving started.");
                    if (dtWebsiteList.Rows.Count > 0)
                    {
                        string fileName = LogPath + "\\" + "summary_" + DateTime.Now.ToString("HHmm dd-MM-yyyy") + ".csv";
                        dtWebsiteList.ToCSV(fileName);
                    }
                    if (dtLiveHistory.Rows.Count > 0)
                    {
                        string fileName = LogPath + "\\" + "history_" + DateTime.Now.ToString("HHmm dd-MM-yyyy") + ".csv";
                        dtLiveHistory.ToCSV(fileName);
                    }
                    Thread.Sleep(300000);
                    clsLogger.WriteToLogFile("Autolog saving finished.");
                }
                catch (Exception ex)
                {
                    clsLogger.WriteToLogFile("Error during auto log saving: " + ex.Message);
                }
            }
        }

        private void DeleteLog()
        {
            while (isAutoLogdeleteEnable)
            {
                try
                {
                    clsLogger.WriteToLogFile("Autolog delete started.");

                    string[] files = Directory.GetFiles(LogPath);

                    foreach (string file in files)
                    {
                        FileInfo fi = new FileInfo(file);
                        if (fi.CreationTime < DateTime.Now.AddDays(-autoLogdeletedays))
                            fi.Delete();
                    }
                    Thread.Sleep(600000);
                    clsLogger.WriteToLogFile("Autolog delete finished.");
                }
                catch (Exception ex)
                {
                    clsLogger.WriteToLogFile("Error during deleting log files : " + ex.Message);
                }
            }
        }

        //private async void BtnSave_Click(object sender, RoutedEventArgs e)
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            //string FindRegex = "(\"viewCount\": \{ \"runs\": \[\{ \"text\": \")(\d+.*)( watching now\" \}\] \}, \"isLive\": true)";
            try
            {
                string WebsiteLink = txtWebsiteLink.Text.Trim();

                if (string.IsNullOrEmpty(WebsiteLink))
                {
                    MessageBox.Show("Website Address Can't be Empty", "No Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!IsUrlValid(WebsiteLink))
                {
                    MessageBox.Show("Invalid website address found", "Invalid Input Data", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string FindRegex = txtFindRegex.Text.Trim();

                if (string.IsNullOrEmpty(FindRegex))
                {
                    MessageBox.Show("Find Regex Can't be Empty", "No Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string Match = txtMatch.Text.Trim();
                if (string.IsNullOrEmpty(Match))
                {
                    MessageBox.Show("Match Can't be Empty", "No Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!int.TryParse(Match, out int found_match))
                {
                    MessageBox.Show("Only intger value allows in Match", "Invalid Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string RetryCount = txtRetryCount.Text.Trim();
                if (string.IsNullOrEmpty(RetryCount))
                {
                    MessageBox.Show("Retry Count Can't be Empty", "No Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!int.TryParse(RetryCount, out int Retry_Count))
                {
                    MessageBox.Show("Only intger value allows in Retry Count", "Invalid Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string RefreshTime = txtRefreshTime.Text.Trim();
                if (string.IsNullOrEmpty(RefreshTime))
                {
                    MessageBox.Show("Refresh Time Can't be Empty", "No Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!int.TryParse(RefreshTime, out int Refresh_Time))
                {
                    MessageBox.Show("Only intger value allows in Refresh Time", "Invalid Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string Remark = txtRemark.Text.Trim();
                if (string.IsNullOrEmpty(Remark))
                {
                    MessageBox.Show("Remark Can't be Empty", "No Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (dtWebsiteList.Rows.Count > 0)
                {
                    int duplicateCount = dtWebsiteList.AsEnumerable().Where(x => x.Field<string>("Remark").ToLower() == Remark.ToLower()).Count();
                    if (duplicateCount > 0)
                    {
                        MessageBox.Show("Remarks can't be duplicate.", "Duplication found", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                //try
                //{
                //    // run a method in another thread
                //    bool isRightWebsiteLink = await Task.Run(() => IsRightWebsiteLink(WebsiteLink));
                //    if (!isRightWebsiteLink)
                //    {
                //        return;
                //    }

                //}
                //catch (Exception ex)
                //{
                //    MessageBox.Show("Error in Website Link : " + ex.Message, "No Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                //    return;
                //}

                DataRow dataRow = dtWebsiteList.NewRow();
                dataRow["Remark"] = Remark;
                dataRow["Live Viewers"] = 0;
                dataRow["Max Viwers"] = 0;
                dataRow["Status"] = "Starting";
                dataRow["Refresh Time"] = Convert.ToInt32(RefreshTime);
                dataRow["Find Regex"] = FindRegex;
                dataRow["Match"] = found_match;
                dataRow["Retry Count"] = Retry_Count;
                dataRow["TStatus"] = "Running";

                dataRow["Website Link"] = WebsiteLink;
                dtWebsiteList.Rows.Add(dataRow);

                Thread HtmlGrabberThread = new Thread(() => ProcessWebsiteData(WebsiteLink, FindRegex, Match, Retry_Count, RefreshTime, Remark));
                HtmlGrabberThread.Name = Remark;
                HtmlGrabberThread.Start();
                threadDictionary.Add(Remark, HtmlGrabberThread);
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during adding data : " + ex.Message);
            }
        }

        //https://stackoverflow.com/questions/27089263/how-to-run-and-interact-with-an-async-task-from-a-wpf-gui
        internal bool IsRightWebsiteLink(string WebsiteLink)
        {
            try
            {
                isWebsiteLinkCheckingTaskRunning = true;
                //Dispatcher.BeginInvoke(new ThreadStart(() => UpdateLbStatus()));
                //Dispatcher.BeginInvoke(new ThreadStart(() => CheckWebsite(WebsiteLink)));
                string htmlCode = string.Empty;
                using (WebClient client = new WebClient())
                {
                    htmlCode = client.DownloadString(WebsiteLink);
                    isWebsiteLinkCheckingTaskRunning = false;
                }
                return isRightWebsiteLink;
            }
            catch (Exception ex)
            {
                LbStatus.Dispatcher.Invoke(() =>
                {
                    // UI operation goes inside of Invoke
                    LbStatus.Content = "";
                    LbStatus.Visibility = Visibility.Hidden;
                });
                isWebsiteLinkCheckingTaskRunning = false;
                MessageBox.Show("Error during website link checking : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
        }

        private void UpdateLbStatus()
        {
            LbStatus.Dispatcher.Invoke(() =>
            {
                // UI operation goes inside of Invoke
                LbStatus.Visibility = Visibility.Visible;
                LbStatus.Content = "Checking website link.";
            });
            while (isWebsiteLinkCheckingTaskRunning)
            {
                // run a method in another thread
                LbStatus.Dispatcher.Invoke(() =>
                {
                    // UI operation goes inside of Invoke
                    LbStatus.Content += ".";
                });

                // CPU-bound or I/O-bound operation goes outside of Invoke
                Thread.Sleep(50);
            }
            LbStatus.Dispatcher.Invoke(() =>
            {
                // UI operation goes inside of Invoke
                LbStatus.Content = "";
                LbStatus.Visibility = Visibility.Hidden;
            });
        }

        private void CheckWebsite(string websiteLink)
        {
            try
            {
                string htmlCode = string.Empty;
                using (WebClient client = new WebClient())
                {
                    htmlCode = client.DownloadString(websiteLink);
                    isWebsiteLinkCheckingTaskRunning = false;
                    isRightWebsiteLink = true;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "The remote server returned an error: (403) Forbidden.")
                {
                    MessageBox.Show("Wrong website address found." + ex.Message, "No Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Error in Website Link : " + ex.Message, "No Input Found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                LbStatus.Dispatcher.Invoke(() =>
                {
                    // UI operation goes inside of Invoke
                    LbStatus.Content = "";
                    LbStatus.Visibility = Visibility.Hidden;
                });
                isWebsiteLinkCheckingTaskRunning = false;
                isRightWebsiteLink = false;
            }
        }

        private bool IsUrlValid(string url)
        {
            string pattern = @"^(http|https|ftp|)\://|[a-zA-Z0-9\-\.]+\.[a-zA-Z](:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*[^\.\,\)\(\s]$";
            Regex reg = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return reg.IsMatch(url);
        }

        private void ProcessWebsiteData(string websiteLink, string findRegex, string _match, int RetryCount, string refreshTime, string remark)
        {
            clsLogger.WriteToLogFile("Processing Website Data Thread Started.");

            string Tstatus = dtWebsiteList.AsEnumerable().Where(x => x.Field<string>("Remark") == remark).Select(x => x.Field<string>("TStatus")).FirstOrDefault();
            int retry_count = RetryCount;
            while (Tstatus != "Stopped" && Tstatus != "Error")
            {
                try
                {
                    DateTime startTime = DateTime.Now;
                    string htmlCode = string.Empty;
                    clsLogger.WriteToLogFile("Downloading : " + remark + " started.");

                    using (WebClient client = new WebClient())
                    {
                        htmlCode = client.DownloadString(websiteLink);
                    }
                    clsLogger.WriteToLogFile("Downloading : " + remark + " finished.");

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
                        else
                        {
                            clsLogger.WriteToLogFile("No result found for : " + remark + ".");
                            Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteStatus(remark, "No result found")));
                        }
                    }
                    DateTime endTime = DateTime.Now;
                    int TimeRemain = Convert.ToInt32((endTime - startTime).TotalSeconds) - Convert.ToInt32(refreshTime);
                    if (TimeRemain < 0)
                    {
                        Thread.Sleep(TimeRemain * -1000);
                    }
                    retry_count = RetryCount;
                }
                catch (Exception ex)
                {
                    clsLogger.WriteToLogFile("Error during process website data for " + remark + " : " + ex.Message);

                    //MessageBox.Show("Error during process website data : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    retry_count--;
                    if (retry_count == 0)
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteStatus(remark, "Error")));
                        Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteTStatus(remark, "Error")));
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteStatus(remark, "Checking")));
                    }
                }
                Tstatus = dtWebsiteList.AsEnumerable().Where(x => x.Field<string>("Remark") == remark).Select(x => x.Field<string>("TStatus")).FirstOrDefault();
            }
        }

        private void UpdateWebsiteTStatus(string remark, string Tstatus)
        {
            try
            {
                IEnumerable<DataRow> rows = dtWebsiteList.Rows.Cast<DataRow>().Where(r => r["Remark"].ToString().ToLower() == remark.ToLower());
                // Loop through the rows and change the name.          
                rows.ToList().ForEach(r => r.SetField("TStatus", Tstatus));
                dataGridWebsiteList.Items.Refresh();
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during updating website TStatus for " + remark + " : " + ex.Message);
            }
        }

        private void UpdateWebsiteStatus(string remark, string status)
        {
            try
            {
                IEnumerable<DataRow> rows = dtWebsiteList.Rows.Cast<DataRow>().Where(r => r["Remark"].ToString().ToLower() == remark.ToLower());
                // Loop through the rows and change the name.          
                rows.ToList().ForEach(r => r.SetField("Status", status));
                dataGridWebsiteList.Items.Refresh();
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during updating website status for " + remark + " : " + ex.Message);
            }
        }

        //https://www.aspsnippets.com/Articles/Calculate-Sum-Total-of-DataTable-Columns-using-C-and-VBNet.aspx
        private void RefreshTotalViewers()
        {
            try
            {
                int TotalViewers = dtWebsiteList.AsEnumerable().Sum(row => row.Field<int>("Max Viwers"));
                LbTotalViewers.Content = TotalViewers.ToString();
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during refresh total viewers method " + ex.Message);
            }
        }

        //https://www.codeproject.com/Questions/362011/Find-and-Update-cell-in-DataTable
        private void RefreshDataGridWebsiteList(string remark, int LiveViewers)
        {
            try
            {
                int maxViewersFound = dtWebsiteList.AsEnumerable().Where(x => x.Field<string>("Remark").ToLower() == remark.ToLower() &&
                          x.Field<int>("Max Viwers") < LiveViewers).Count();

                //DataRow dr = dtWebsiteList.Select("Remark='" + remark + "'").Max();
                string Tstatus = dtWebsiteList.AsEnumerable().Where(x => x.Field<string>("Remark").ToLower() == remark.ToLower()).Select(x => x.Field<string>("TStatus")).FirstOrDefault();
                if (maxViewersFound > 0)
                {
                    // Get all DataRows where the name is the name you want.
                    IEnumerable<DataRow> rows = dtWebsiteList.Rows.Cast<DataRow>().Where(r => r["Remark"].ToString().ToLower() == remark.ToLower());
                    // Loop through the rows and change the name.
                    rows.ToList().ForEach(r => r.SetField("Max Viwers", LiveViewers));
                    rows.ToList().ForEach(r => r.SetField("Live Viewers", LiveViewers));
                    if (Tstatus != "Stopped")
                        Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteStatus(rows.ToList().Select(r => r.Field<string>("Remark")).FirstOrDefault(), "Active")));
                    //rows.ToList().ForEach(r => r.SetField("Status", "Active"));
                    dataGridWebsiteList.Items.Refresh();
                }
                else
                {
                    // Get all DataRows where the name is the name you want.
                    IEnumerable<DataRow> rows = dtWebsiteList.Rows.Cast<DataRow>().Where(r => r["Remark"].ToString().ToLower() == remark.ToLower());
                    // Loop through the rows and change the name.                
                    rows.ToList().ForEach(r => r.SetField("Live Viewers", LiveViewers));
                    //rows.ToList().ForEach(r => r.SetField("Status", "Active"));
                    if (Tstatus != "Stopped")
                        Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteStatus(rows.ToList().Select(r => r.Field<string>("Remark")).FirstOrDefault(), "Active")));

                    dataGridWebsiteList.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during refreshing website list data for : " + remark + " : " + ex.Message);
            }
        }

        private void RefreshDataGridWebsiteList()
        {
            try
            {
                dataGridWebsiteList.Items.Refresh();
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during refreshing website grid data :" + ex.Message);
            }
        }

        private void RefreshDataGridHistory()
        {
            try
            {
                DataGridHistory.Items.Refresh();
                DataGridHistory.ScrollIntoView(CollectionView.NewItemPlaceholder);
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during refreshing website history data :" + ex.Message);
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
            try
            {
                dtLiveHistory.Clear();
                RefreshDataGridHistory();
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during clearing history :" + ex.Message);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                Environment.Exit(Environment.ExitCode);
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during window closed event :" + ex.Message);
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during exporting data :" + ex.Message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                e.Cancel = MessageBox.Show("Are you sure to exit application?", "Exit Application", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No;
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during window closing event :" + ex.Message);
            }
        }

        //https://stackoverflow.com/questions/18854395/how-to-delete-rows-from-datatable-with-linq
        private void DataGridWebsiteList_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (sender is DataGrid dg)
                {
                    DataGridRow dgr = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(dg.SelectedIndex);
                    if (e.Key == Key.Delete && !dgr.IsEditing)
                    {
                        // User is attempting to delete the row
                        var result = MessageBox.Show(
                            "Are you sure to delete selected website from watch list ?",
                            "Delete",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question,
                            MessageBoxResult.No);

                        //((DataRowView)dgr.Item).Row.ItemArray[0];
                        string Remark = ((DataRowView)dgr.Item).Row.Field<string>("Remark").ToString();
                        dtWebsiteList.AsEnumerable().Where(x => x.Field<string>("Remark") == Remark).ToList().ForEach(x => x.Delete());
                        dtLiveHistory.AsEnumerable().Where(x => x.Field<string>("Remark") == Remark).ToList().ForEach(x => x.Delete());
                        //e.Handled = (result == MessageBoxResult.No);
                        RefreshDataGridHistory();
                        RefreshDataGridWebsiteList();
                        RefreshTotalViewers();
                    }
                }
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during deleting data :" + ex.Message);
            }
        }

        //https://stackoverflow.com/questions/3286583/how-to-add-context-menu-to-wpf-datagrid
        //https://stackoverflow.com/questions/16822956/getting-wpf-data-grid-context-menu-click-row
        //https://stackoverflow.com/questions/19288845/aborting-a-thread-via-its-name
        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Get the clicked MenuItem
                var menuItem = (MenuItem)sender;

                //Get the ContextMenu to which the menuItem belongs
                var contextMenu = (ContextMenu)menuItem.Parent;

                //Find the placementTarget
                var item = (DataGrid)contextMenu.PlacementTarget;

                DataGridRow dgr = (DataGridRow)item.ItemContainerGenerator.ContainerFromIndex(item.SelectedIndex);

                string Remark = ((DataRowView)dgr.Item).Row.Field<string>("Remark").ToString();
                //Not supported in dotnet core
                //threadDictionary[Remark].Abort();
                Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteStatus(Remark, "Stopped")));
                Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteStatus(Remark, "Stopped")));
                Dispatcher.BeginInvoke(new ThreadStart(() => AbortThreadProcessWebsiteData(Remark)));
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during deleting data :" + ex.Message);
            }
        }

        private void AbortThreadProcessWebsiteData(string Remark)
        {
            try
            {
                while (threadDictionary[Remark].IsAlive)
                {
                    Thread.Sleep(100);
                }
                threadDictionary.Remove(Remark);
                dtWebsiteList.AsEnumerable().Where(x => x.Field<string>("Remark") == Remark).ToList().ForEach(x => x.Delete());
                dtLiveHistory.AsEnumerable().Where(x => x.Field<string>("Remark") == Remark).ToList().ForEach(x => x.Delete());

                RefreshDataGridHistory();
                RefreshDataGridWebsiteList();
                RefreshTotalViewers();
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during aborting thread :" + ex.Message);
            }
        }

        [Obsolete]
        private void MenuItem_Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Get the clicked MenuItem
                var menuItem = (MenuItem)sender;

                //Get the ContextMenu to which the menuItem belongs
                var contextMenu = (ContextMenu)menuItem.Parent;

                //Find the placementTarget
                var item = (DataGrid)contextMenu.PlacementTarget;

                DataGridRow dgr = (DataGridRow)item.ItemContainerGenerator.ContainerFromIndex(item.SelectedIndex);

                string Remark = ((DataRowView)dgr.Item).Row.Field<string>("Remark").ToString();

                if (threadDictionary.ContainsKey(Remark))
                {
                    if (threadDictionary[Remark].IsAlive)
                    {
                        MessageBox.Show("Website is already active.", "Process already running.", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                string WebsiteLink = ((DataRowView)dgr.Item).Row.Field<string>("Website Link").ToString();
                string FindRegex = ((DataRowView)dgr.Item).Row.Field<string>("Find Regex").ToString();
                string Match = ((DataRowView)dgr.Item).Row.Field<int>("Match").ToString();
                int Retry_Count = Convert.ToInt32(((DataRowView)dgr.Item).Row.Field<int>("Retry Count"));
                string RefreshTime = ((DataRowView)dgr.Item).Row.Field<int>("Refresh Time").ToString();
                Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteTStatus(Remark, "Running")));
                Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteStatus(Remark, "Starting")));

                Thread.Sleep(1000);
                Thread HtmlGrabberThread = new Thread(() => ProcessWebsiteData(WebsiteLink, FindRegex, Match, Retry_Count, RefreshTime, Remark));
                HtmlGrabberThread.Name = Remark;
                HtmlGrabberThread.Start();
                threadDictionary.Add(Remark, HtmlGrabberThread);
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during starting thread on menu click :" + ex.Message);
            }
        }

        private void MenuItem_Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Get the clicked MenuItem
                var menuItem = (MenuItem)sender;

                //Get the ContextMenu to which the menuItem belongs
                var contextMenu = (ContextMenu)menuItem.Parent;

                //Find the placementTarget
                var item = (DataGrid)contextMenu.PlacementTarget;

                DataGridRow dgr = (DataGridRow)item.ItemContainerGenerator.ContainerFromIndex(item.SelectedIndex);

                string Remark = ((DataRowView)dgr.Item).Row.Field<string>("Remark").ToString();

                //Not supported in dotnet core
                //threadDictionary[Remark].Abort();
                threadDictionary.Remove(Remark);
                Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteTStatus(Remark, "Stopped")));
                Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteStatus(Remark, "Stopped")));
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during stoping thread on menu click :" + ex.Message);
            }
        }

        private void MenuItem_Reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Get the clicked MenuItem
                var menuItem = (MenuItem)sender;

                //Get the ContextMenu to which the menuItem belongs
                var contextMenu = (ContextMenu)menuItem.Parent;

                //Find the placementTarget
                var item = (DataGrid)contextMenu.PlacementTarget;

                DataGridRow dgr = (DataGridRow)item.ItemContainerGenerator.ContainerFromIndex(item.SelectedIndex);

                string Remark = ((DataRowView)dgr.Item).Row.Field<string>("Remark").ToString();

                Dispatcher.BeginInvoke(new ThreadStart(() => UpdateWebsiteMaxViweres(Remark)));
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during reseting max count on menu click :" + ex.Message);
            }
        }

        private void UpdateWebsiteMaxViweres(string remark)
        {
            try
            {
                IEnumerable<DataRow> rows = dtWebsiteList.Rows.Cast<DataRow>().Where(r => r["Remark"].ToString().ToLower() == remark.ToLower());
                // Loop through the rows and change the name.          
                rows.ToList().ForEach(r => r.SetField("Max Viwers", 0));
                dataGridWebsiteList.Items.Refresh();
            }
            catch (Exception ex)
            {
                clsLogger.WriteToLogFile("Error during updating max viewers of : " + remark + " :" + ex.Message);
            }
        }
    }
}
