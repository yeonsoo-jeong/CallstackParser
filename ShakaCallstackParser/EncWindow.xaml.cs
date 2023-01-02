using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShakaCallstackParser
{
    /// <summary>
    /// EncWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EncWindow : Window
    {
        EncodeManager enc_manager_;
        List<EncListItems> result = new List<EncListItems>();

        const string kBtnLabelEncode = "Encode";
        const string kBtnLabelCancel = "Cancel";

        public EncWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            ListView1.AllowDrop = true;
            EncodeManager.Callbacks callback = new EncodeManager.Callbacks(EncodeProgressChanged, 
                EncodeFinished, AllEncodeFinished, SSIMCalculateFinished, AnalyzeFinished);
            enc_manager_ = new EncodeManager(callback);
        }

        private void ReorderEncListNumber()
        {
            for (int i = 0; i < result.Count; i++)
            {
                result[i].number = i.ToString();
            }
            ListView1.Items.Refresh();
        }

        private void ListView1_OnDroped(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //result.Clear();

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length >= 1)
                {
                    foreach (string file in files)
                    {
                        EncListItems item = new EncListItems();
                        item.path = file;
                        item.note = "core=" + Environment.ProcessorCount.ToString();
                        item.cpu_usage = new List<string>()
                        {
                            "Full",
                            "Half"
                        };
                        item.cpu_usage_selected = "Full";
                        result.Add(item);
                    }

                    result = result.Distinct(new EncListComparer()).ToList();
                    ReorderEncListNumber();

                    ListView1.ItemsSource = result;
                    ListView1.Items.Refresh();
                }
            }
        }

        private void TempWriteResult()
        {
            for (int i = 0; i < result.Count; i++)
            {
                Loger.Write("result[" + i + "]: index, number, combo, path=" + i + ", " + result[i].number + ", " + result[i].cpu_usage_selected + ", " + result[i].path);
            }
            Loger.Write("");
        }

        private void Btn1_Click(object sender, RoutedEventArgs e)
        {
            TempWriteResult();

            if (Btn1.Content.ToString() == kBtnLabelEncode)
            {
                if (ListView1.Items.Count > 0)
                {
                    Btn1.IsEnabled = false;

                    List<EncodeJob> jobs = new List<EncodeJob>();
                    for (int i = 0; i < result.Count; i++)
                    {
                        if (result[i].status != EncListItems.Status.success)
                        {
                            if (result[i].status == EncListItems.Status.none)
                            {
                                // for cancel & restart scenario
                                result[i].note = "";
                                result[i].progress = 0;
                            }

                            //string path = ((EncListItems)ListView1.Items[i]).path;
                            //jobs.Add(new EncodeJob(i, path));
                            jobs.Add(new EncodeJob(i, result[i].path, EncodeManager.GetCoreNumFromCpuUsage(result[i].cpu_usage_selected)));
                            
                        }
                    }
                    enc_manager_.Start(jobs);
                    Btn1.Content = kBtnLabelCancel;
                    ListView1.IsEnabled = false;
                    BtnRemoveDone.IsEnabled = false;
                    ListView1.Items.Refresh();

                    Task.Delay(1000).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Btn1.IsEnabled = true;
                        });
                    });
                }
                else
                {
                    MessageBox.Show("인코딩할 항목이 존재하지 않습니다."); 
                }
            }
            else if (Btn1.Content.ToString() == kBtnLabelCancel)
            {
                Btn1.IsEnabled = false;

                enc_manager_.OnEncodeCanceled();
                Btn1.Content = kBtnLabelEncode;
                ListView1.IsEnabled = true;
                BtnRemoveDone.IsEnabled = true;

                Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        Btn1.IsEnabled = true;
                    });
                });
            }
            else
            {
                // Unexpected Scenario
                Loger.Write("EncWindow.xaml.cs : Btn1_Click : Unexpected Scenario");
            }
        }

        private void SSIMCalculateFinished(int index, int crf, double ssim)
        {
            Dispatcher.Invoke(() =>
            {
                string strSSIM = Math.Round(ssim, 4).ToString();
                string msg = crf.ToString() + ":" + strSSIM;
                if (result[index].note.Length == 0)
                {
                    result[index].note = msg;
                }
                else
                {
                    result[index].note += ", " + msg;
                }
                ListView1.Items.Refresh();
            });
        }

        private void AnalyzeFinished(int index, int crf)
        {
            Dispatcher.Invoke(() =>
            {
                result[index].note += " => " + crf.ToString();
                ListView1.Items.Refresh();
            });
        }

        private void EncodeProgressChanged(int index, int percentage)
        {
            Dispatcher.Invoke(() => 
            {
                result[index].progress = percentage;
                ListView1.Items.Refresh();
            });
        }

        private void EncodeFinished(int index, int result_code)
        {
            Dispatcher.Invoke(() =>
            {
                if (result_code == 0)
                {
                    result[index].note = result[index].note + ", Success[" + result_code.ToString() + "]";
                    result[index].status = EncListItems.Status.success;
                }
                else
                {
                    result[index].note = result[index].note + ", Fail[" + result_code.ToString() + "]";
                    result[index].status = EncListItems.Status.fail;
                }
                ListView1.Items.Refresh();
            });
        }

        private void AllEncodeFinished()
        {
            Dispatcher.Invoke(() =>
            {
                Btn1.Content = kBtnLabelEncode;
                ListView1.IsEnabled = true;
                BtnRemoveDone.IsEnabled = true;
            });
        }

        private void Window_Activated(object sender, EventArgs e)
        {
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            enc_manager_.OnWindowClosed();
            //MessageBox.Show("Closed");
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Loger.Write("e.RoutedEvent.Name=" + e.RoutedEvent.Name);
            foreach (EncListItems item in ListView1.SelectedItems)
            {
                result.Remove(item);
            }
            ReorderEncListNumber();
            ListView1.Items.Refresh();
        }

        private void BtnRemoveDone_Click(object sender, RoutedEventArgs e)
        {
            for (int i = result.Count - 1; i >= 0; i--)
            {
                if (result[i].status == EncListItems.Status.success)
                {
                    result.RemoveAt(i);
                }
            }
            ReorderEncListNumber();
            ListView1.Items.Refresh();
        }
    }

    public class EncListItems
    {
        public enum Status
        {
            none,
            success,
            fail
        }

        public EncListItems()
        {
            progress = 0;
            status = Status.none;
        }

        public string number { get; set; }
        public string path { get; set; }
        public int progress { get; set; }

        public List<string> cpu_usage { get; set; }

        public string cpu_usage_selected { get; set; }

        public string note { get; set; }
        public Status status { get; set; }
    }

    public class EncListComparer : IEqualityComparer<EncListItems>
    {
        public bool Equals(EncListItems x, EncListItems y)
        {
            return x.path == y.path;
        }

        public int GetHashCode(EncListItems obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return obj.path == null ? 0 : obj.path.GetHashCode();
        }
    }

    //public class SpeedComboBoxEntry
    //{
    //    private static readonly List<string> speed;

    //    static SpeedComboBoxEntry()
    //    {
    //        speed = new List<string>();
    //        speed.Add("full");
    //        speed.Add("half");
    //    }

    //    public IEnumerable<string> SpeedSource => speed;
    //}
}
