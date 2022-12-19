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

        private void ListView1_OnDroped(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //result.Clear();

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length >= 1)
                {
                    int num = 1;
                    if (result.Count() > 0)
                    {
                        num = Int32.Parse(result[result.Count() - 1].number) + 1;
                    }

                    foreach (string file in files)
                    {
                        EncListItems item = new EncListItems();
                        item.number = num.ToString();
                        item.path = file;
                        item.progress = 0;
                        //item.note = "";
                        item.note = "core=" + Environment.ProcessorCount.ToString();
                        result.Add(item);
                        num++;
                    }

                    result = result.Distinct(new EncListComparer()).ToList();

                    ListView1.ItemsSource = result;
                    ListView1.Items.Refresh();
                }
            }
        }

        private void Btn1_Click(object sender, RoutedEventArgs e)
        {
            if (ListView1.Items.Count > 0)
            {
                int count = ListView1.Items.Count;
                List<EncodeJob> jobs = new List<EncodeJob>();
                for (int i = 0; i < count; i++)
                {
                    string path = ((EncListItems)ListView1.Items[i]).path;
                    jobs.Add(new EncodeJob(i, path));
                }
                enc_manager_.Start(jobs);
                Btn1.Content = "Encoding";
                Btn1.IsEnabled = false;
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
                }
                else
                {
                    result[index].note = result[index].note + ", Fail[" + result_code.ToString() + "]";
                }
                ListView1.Items.Refresh();
            });
        }

        private void AllEncodeFinished()
        {
            Dispatcher.Invoke(() =>
            {
                Btn1.Content = "Finished";
            });
        }
    }

    public class EncListItems
    {
        public string number { get; set; }
        public string path { get; set; }
        public int progress { get; set; }
        public string note { get; set; }
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
}
