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
        EncItemManager enc_item_manager_;
        
        const string kBtnLabelEncode = "Encode";
        const string kBtnLabelCancel = "Cancel";

        public EncWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            EncodeManager.Callbacks callback = new EncodeManager.Callbacks(EncodeProgressChanged, 
                EncodeFinished, AllEncodeFinished, SSIMCalculateFinished, AnalyzeFinished);
            enc_manager_ = new EncodeManager(callback);
            enc_item_manager_ = new EncItemManager();

            EncodingFileManager.DeleteAllTempFiles();
        }

        private void ListView1_OnDroped(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length >= 1)
                {
                    enc_item_manager_.DistinctAddFiles(files);
                    ListView1.ItemsSource = enc_item_manager_.GetEncItems();
                    ListView1.Items.Refresh();
                }
            }
        }

        private void TempWriteResult()
        {
            List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
            for (int i = 0; i < enc_items.Count; i++)
            {
                Loger.Write("result[" + i + "]: index, number, combo, path=" + i + ", " + enc_items[i].number + ", " + enc_items[i].cpu_usage_selected + ", " + enc_items[i].path);
            }
            Loger.Write("");
        }

        private void Btn1_Click(object sender, RoutedEventArgs e)
        {
            TempWriteResult();

            if (Btn1.Content.ToString() == kBtnLabelEncode)
            {
                if (enc_item_manager_.GetToEncodeItemsNum() > 0)
                {
                    Start();
                }
                else
                {
                    MessageBox.Show("인코딩할 항목이 존재하지 않습니다.");
                }
            }
            else if (Btn1.Content.ToString() == kBtnLabelCancel)
            {
                Cancel();
            }
            else
            {
                // Unexpected Scenario
                Loger.Write("EncWindow.xaml.cs : Btn1_Click : Unexpected Scenario");
            }
        }

        private void Start()
        {
            Btn1.IsEnabled = false;

            enc_item_manager_.Refresh();
            List<EncodeJob> jobs = enc_item_manager_.GetToEncodeJobs();

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

        private void Cancel()
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

        private void SSIMCalculateFinished(int index, int crf, double ssim)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();

                string strSSIM = Math.Round(ssim, 4).ToString();
                string msg = crf.ToString() + ":" + strSSIM;
                if (enc_items[index].note.Length == 0)
                {
                    enc_items[index].note = msg;
                }
                else
                {
                    enc_items[index].note += ", " + msg;
                }
                ListView1.Items.Refresh();
            });
        }

        private void AnalyzeFinished(int index, int crf)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();

                enc_items[index].note += " => " + crf.ToString();
                ListView1.Items.Refresh();
            });
        }

        private void EncodeProgressChanged(int index, int percentage)
        {
            Dispatcher.Invoke(() => 
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();

                enc_items[index].progress = percentage;
                ListView1.Items.Refresh();
            });
        }

        private void EncodeFinished(int index, int result_code)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();

                if (result_code == 0)
                {
                    enc_items[index].note = enc_items[index].note + ", Success[" + result_code.ToString() + "]";
                    enc_items[index].status = EncListItems.Status.success;
                }
                else
                {
                    enc_items[index].note = enc_items[index].note + ", Fail[" + result_code.ToString() + "]";
                    enc_items[index].status = EncListItems.Status.fail;
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
            List<EncListItems> items = new List<EncListItems>();
            foreach (EncListItems item in ListView1.SelectedItems)
            {
                items.Add(item);
            }
            enc_item_manager_.RemoveItems(items);
            ListView1.Items.Refresh();
        }

        private void BtnRemoveDone_Click(object sender, RoutedEventArgs e)
        {
            enc_item_manager_.RemoveFinishedItems();
            ListView1.Items.Refresh();
        }

        private void BtnOpenDestPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                TextDestPath.Text = dialog.SelectedPath;
            }
        }
    }
}
