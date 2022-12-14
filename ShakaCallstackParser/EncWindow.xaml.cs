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
            enc_manager_ = new EncodeManager(EncodeProgressChanged, EncodeFinished, SSIMCalculateFinished, AnalyzeFinished);
        }

        private void ListView1_OnDroped(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                result.Clear();

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length >= 1)
                {
                    int num = 1;
                    foreach (string file in files)
                    {
                        EncListItems item = new EncListItems();
                        item.number = num.ToString();
                        item.path = file;
                        item.progress = 0;
                        item.note = "";
                        result.Add(item);
                        num++;
                    }

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

        private void EncodeFinished(int index)
        {
            //Dispatcher.Invoke(() =>
            //{
            //    result[index].progress = 100;
            //    ListView1.Items.Refresh();
            //});
        }
    }

    public class EncListItems
    {
        public string number { get; set; }
        public string path { get; set; }
        public int progress { get; set; }
        public string note { get; set; }
    }
}
