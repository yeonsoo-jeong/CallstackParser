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

        public static string[] kCpuUsageItems =
        {
            "Full",
            "Half"
        };

        public EncWindow()
        {
            InitializeComponent();
            Init();
        }


        // Todo. 
        // 1. Deinterlace Filter
        // 




        private void Init()
        {
            EncodeManager.Callbacks callback = new EncodeManager.Callbacks(OnEncodeStarted,
                OnEncodeProgressChanged, OnEncodeFinished, OnAllEncodeFinished, OnEncodeCancled,
                OnAnalyzeStarted, OnAnalyzeFinished);
            enc_manager_ = new EncodeManager(callback);
            enc_item_manager_ = new EncItemManager();

            TextBoxDestPath.Text = ConfigManager.GetDestPath();

            for (int i = 0; i < kCpuUsageItems.Length; i++)
            {
                ComboUsageAll.Items.Add(kCpuUsageItems[i]);
            }
            ComboUsageAll.SelectedIndex = 0;

            EncodingFileManager.DeleteAllTempFiles();
        }

        private void ListView1_OnDroped(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length >= 1)
                {
                    enc_item_manager_.DistinctAddFiles(files, ComboUsageAll.SelectedItem.ToString());
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

        private void BtnEncodeCancel_Click(object sender, RoutedEventArgs e)
        {
            TempWriteResult();

            if (BtnEncodeCancel.Content.ToString() == kBtnLabelEncode)
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
            else if (BtnEncodeCancel.Content.ToString() == kBtnLabelCancel)
            {
                Cancel();
            }
            else
            {
                // Unexpected Scenario
                Loger.Write("EncWindow.xaml.cs : BtnEncodeCancel_Click : Unexpected Scenario");
            }
        }

        private void Start()
        {
            BtnEncodeCancel.IsEnabled = false;

            enc_item_manager_.Refresh();
            List<EncodeJob> jobs = enc_item_manager_.GetToEncodeJobs();

            Task.Run(() => enc_manager_.Start(jobs, TextBoxDestPath.Text));

            BtnEncodeCancel.Content = kBtnLabelCancel;
            BtnRemoveDone.IsEnabled = false;
            BtnOpenDestPath.IsEnabled = false;
            ListView1.IsEnabled = false;
            ListView1.Items.Refresh();

            Task.Delay(1000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    BtnEncodeCancel.IsEnabled = true;
                });
            });
        }

        private void Cancel()
        {
            BtnEncodeCancel.IsEnabled = false;

            enc_manager_.OnEncodeCanceled();
            BtnEncodeCancel.Content = kBtnLabelEncode;
            ListView1.IsEnabled = true;
            BtnRemoveDone.IsEnabled = true;
            BtnOpenDestPath.IsEnabled = true;

            Task.Delay(1000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    BtnEncodeCancel.IsEnabled = true;
                });
            });
        }

        #region Analyzer Callback
        private void OnAnalyzeStarted(int index)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
                enc_items[index].note = "Analyzing";
                ListView1.Items.Refresh();
            });
        }

        private void OnAnalyzeFinished(int index, int crf)
        {
            Dispatcher.Invoke(() =>
            {
                //List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
                //enc_items[index].note += " => " + crf.ToString();
                //ListView1.Items.Refresh();
            });
        }
        #endregion

        #region Encoder Callback
        private void OnEncodeStarted(int index, int crf)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
                enc_items[index].note = "Encoding";
                ListView1.Items.Refresh();
            });
        }

        private void OnEncodeProgressChanged(int index, int percentage)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();

                enc_items[index].progress = percentage;
                ListView1.Items.Refresh();
            });
        }

        private void OnEncodeFinished(int index, int result_code)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();

                if (result_code == 0)
                {
                    enc_items[index].note = "Success";
                    enc_items[index].status = EncListItems.Status.success;
                }
                else
                {
                    enc_items[index].note = "Fail";
                    enc_items[index].status = EncListItems.Status.fail;
                }
                ListView1.Items.Refresh();
            });
        }

        private void OnAllEncodeFinished()
        {
            Dispatcher.Invoke(() =>
            {
                BtnEncodeCancel.Content = kBtnLabelEncode;
                ListView1.IsEnabled = true;
                BtnRemoveDone.IsEnabled = true;
                BtnOpenDestPath.IsEnabled = true;
            });
        }

        private void OnEncodeCancled(int index)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
                enc_items[index].note = "Canceled";
                ListView1.Items.Refresh();
            });
        }
        #endregion

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
                TextBoxDestPath.Text = dialog.SelectedPath;
                ConfigManager.SetDestPath(dialog.SelectedPath);
            }
        }

        private void ComboUsageAll_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            enc_item_manager_.OnCpuUsageChanged((sender as ComboBox).SelectedItem.ToString());
            ListView1.Items.Refresh();
        }
    }
}
