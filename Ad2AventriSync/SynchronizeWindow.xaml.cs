using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Ad2AventriSync
{
    /// <summary>
    /// Interaction logic for SynchronizeWindow.xaml
    /// </summary>
    public partial class SynchronizeWindow : Window
    {
        private object _lock = new object();
        protected bool _isExporting = false; 
        public bool IsExporting
        {
            get
            {
                lock (_lock)
                {
                    return _isExporting;
                }
            }
        }

        public SynchronizeWindow()
        {
            InitializeComponent();
        }

        protected void WriteLineToLogBox(string text, bool setTitle=false, Brush color = null)
        {
            if (setTitle)
            {
                ProgressText.Text = text;
            }
            Paragraph p = new Paragraph();
            if(setTitle)
            {
                p.Foreground = Brushes.Blue;
                p.FontSize += 4;
            }
            else
            {
                if(color == null)
                {
                    p.Foreground = Brushes.Black;
                }
                else
                {
                    p.Foreground = color;
                }
            }
            p.Margin = new Thickness(0);

            p.Inlines.Add(text);
            LogBox.Document.Blocks.Add(p);
            LogBox.ScrollToEnd();
        }


        public async void ExportAdToAventri(string query, string baseDN, string aventriHost, string aventriAccountId, string aventriApiKey, string proxyUri, string subscriberId)
        {
            lock(_lock)
            {
                if (_isExporting) return;
                _isExporting = true;
            }
            try
            {
                LogBox.Document = new FlowDocument();
                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = 100;

                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                Task progressbarTask = Task.Run(
                    async () => {
                        int i = 1;
                        while(i <= 100 && !ct.IsCancellationRequested)
                        {
                            this.Dispatcher.Invoke(() => { ProgressBar.Value = i; });
                            await Task.Delay(800);
                            i++;
                        }
                    },
                    ct
                );

                WriteLineToLogBox("Step 1 of 4: Reading users from AD. This might take a while...", true);
                WriteLineToLogBox("LDAP BaseDN:\t" + baseDN + "\nLDAP Query:\t" + query);
                Task<List<Dictionary<string, List<string>>>> adTask = new Task<List<Dictionary<string, List<string>>>>(delegate() {
                    AdSearcher ads = new AdSearcher();
                    ads.BaseDn = baseDN;
                    ads.PropertiesToLoad = new List<string> {
                        "cn",
                        "displayName",
                        "GivenName",
                        "sn",
                        "Mail",
                        "Department",
                        "physicalDeliveryOfficeName",
                        "co",
                        "l",
                        "sAMAccountName"
                    };
                    return ads.Search(query);
                });
                adTask.Start();
                List<Dictionary<string, List<string>>> adUsers = await adTask;
                WriteLineToLogBox("Loaded " + adUsers.Count + " users from AD.");

                WriteLineToLogBox("Step 2 of 4: Reading subscribers from Aventri. This might take a while...", true);
                WriteLineToLogBox(((proxyUri == null || proxyUri == "") ? "HTTP Proxy Url:\t" + proxyUri + "\n" : "") + "Aventri Host:\t\t" + aventriHost + "\nAventri Account ID:\t" + aventriAccountId + "\nSubscriber List ID:\t\t" + subscriberId);
                AventriRest rest = new AventriRest(aventriHost, aventriAccountId, aventriApiKey, proxyUri);
                Task<Dictionary<string, Dictionary<string, string>>> aventriTask = new Task<Dictionary<string, Dictionary<string, string>>>(delegate () { return rest.GetAllSubscribersAsDict(subscriberId); });
                aventriTask.Start();
                Dictionary<string, Dictionary<string, string>> aventriSubscribers = await aventriTask;
                WriteLineToLogBox("Loaded " + aventriSubscribers.Count + " subscribers from Aventri.");

                cts.Cancel();
                await progressbarTask;

                ProgressBar.Value = 0;
                ProgressBar.Maximum = adUsers.Count;
                Dictionary<string, bool> emailsInAd = new Dictionary<string, bool>();
                WriteLineToLogBox("Step 3 of 4: Adding new subscribers to Aventri. This might take a while...", true);
                int subscribersAdded = 0;
                await Task.Run(
                    () => {
                        foreach (Dictionary<string, List<string>> aduser in adUsers)
                        {
                            this.Dispatcher.Invoke(() => { ProgressBar.Value += 1; });

                            if (aduser["mail"].Count <= 0)
                            {
                                this.Dispatcher.Invoke(() => {
                                    string cn = "<object has no cn>";
                                    if (aduser.ContainsKey("cn") && aduser["cn"].Count > 0) cn = aduser["cn"][0];
                                    WriteLineToLogBox("The user has no email address. Skipping user: " + cn);
                                });
                                continue;
                            }

                            string mail = aduser["mail"][0].ToLower();
                            emailsInAd[mail] = true;
                            if(!aventriSubscribers.ContainsKey(mail))
                            {
                                this.Dispatcher.Invoke(() => {
                                    WriteLineToLogBox("Adding subscriber to aventri with mail: " + mail, false, Brushes.Green);
                                });
                                subscribersAdded++;
                                rest.AddSubscriber(
                                    subscriberId,
                                    aduser["mail"][0],
                                    (aduser["givenname"].Count > 0 ? aduser["givenname"][0] : ""),
                                    (aduser["sn"].Count > 0 ? aduser["sn"][0] : ""),
                                    (aduser["l"].Count > 0 ? aduser["l"][0] : ""),
                                    (aduser["co"].Count > 0 ? aduser["co"][0] : ""),
                                    (aduser["samaccountname"].Count > 0 ? aduser["samaccountname"][0] : "")
                                );
                            }
                        } 
                    }
                );
                WriteLineToLogBox("Added " + subscribersAdded + " subscribers.");

                ProgressBar.Value = 0;
                ProgressBar.Maximum = aventriSubscribers.Count;
                WriteLineToLogBox("Step 4 of 4: Deleting orphaned subscribers from Aventri. This might take a while...", true);
                int subscribersDeleted = 0;
                await Task.Run(
                    () => {
                        foreach (string mail in aventriSubscribers.Keys)
                        {
                            this.Dispatcher.Invoke(() => { ProgressBar.Value += 1; });

                            if(!emailsInAd.ContainsKey(mail))
                            {
                                string subscriberid = "";
                                if (aventriSubscribers[mail].ContainsKey("subscriberid"))
                                {
                                    subscriberid = aventriSubscribers[mail]["subscriberid"];
                                }
                                if (subscriberid == "") continue;

                                this.Dispatcher.Invoke(() => {
                                    WriteLineToLogBox("Deleting subscriber \"" + subscriberid + "\" from aventri with mail: " + mail, false, Brushes.DarkOrange);
                                });
                                subscribersDeleted++;
                                rest.DeleteSubscriber(subscriberid);
                            }
                        }
                    }
                );
                WriteLineToLogBox("Deleted " + subscribersDeleted + " subscribers.");

                WriteLineToLogBox("Export has been successfully comitted.", true);
                ProgressText.Text = "";
            }
            catch(Exception e)
            {
                WriteLineToLogBox("Export to Aventri failed with exception:\n" + e.Message, false, Brushes.Red);
                ProgressText.Text = "";
            }
            finally
            {
                lock (_lock)
                {
                    _isExporting = false;
                }
            }
        }
    }
}
