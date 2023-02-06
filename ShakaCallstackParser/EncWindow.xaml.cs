using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using System.IO;

using YsCommon;

namespace ShakaCallstackParser
{
    /// <summary>
    /// EncWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EncWindow : Window
    {
        EncodeManager enc_manager_;
        EncItemManager enc_item_manager_;

        const string TAG = "EncWindow.xaml.cs : ";
        const string kBtnLabelEncode = "Encode";
        const string kBtnLabelCancel = "Cancel";

        bool is_window_closed_ = false;

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
                OnEncodeProgressChanged, OnEncodeCanceled, OnEncodeFailed, OnEncodeFinished, OnAllEncodeFinished,
                OnAnalyzeStarted, OnAnalyzeCanceled, OnAnalyzeFailed, OnAnalyzeFinished);
            
            enc_item_manager_ = new EncItemManager();
            enc_manager_ = new EncodeManager(callback, enc_item_manager_);

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

        private void BtnEncodeCancel_Click(object sender, RoutedEventArgs e)
        {
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

            string dest_path = TextBoxDestPath.Text;
            Task.Run(() => enc_manager_.Start(dest_path));

            BtnEncodeCancel.Content = kBtnLabelCancel;
            BtnRemoveDone.IsEnabled = false;
            BtnChangeDestPath.IsEnabled = false;
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
            BtnRemoveDone.IsEnabled = true;
            BtnChangeDestPath.IsEnabled = true;

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
                enc_items[index].status = EncListItems.Status.analyzing;
                ListView1.Items.Refresh();
                Loger.Write(TAG + "OnAnalyzeStarted : " + Path.GetFileName(enc_items[index].path));
            });
        }

        private void OnAnalyzeCanceled(int index)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
                enc_items[index].note = "Canceled";
                enc_items[index].status = EncListItems.Status.cancel;
                ListView1.Items.Refresh();
                Loger.Write(TAG + "OnAnalyzeCanceled : " + Path.GetFileName(enc_items[index].path) + "\r\n");
            });
        }

        private void OnAnalyzeFailed(int index, string msg)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
                enc_items[index].note = "Analyzation Failed";
                if (msg.Length > 0)
                {
                    enc_items[index].note = msg;
                }
                enc_items[index].progress = 100;
                enc_items[index].progress_color = "Red";
                enc_items[index].status = EncListItems.Status.fail;
                ListView1.Items.Refresh();
                Loger.Write(TAG + "OnAnalyzeFailed : " + Path.GetFileName(enc_items[index].path) + "\r\n");
            });
        }

        private void OnAnalyzeFinished(int index, int crf)
        {
            Dispatcher.Invoke(() =>
            {
                //List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
                //enc_items[index].note += " => " + crf.ToString();
                //ListView1.Items.Refresh();

                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
                Loger.Write(TAG + "OnAnalyzeFinished : " + Path.GetFileName(enc_items[index].path) + ", crf=" + crf);
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
                enc_items[index].status = EncListItems.Status.encoding;
                ListView1.Items.Refresh();
                Loger.Write(TAG + "OnEncodeStarted : " + Path.GetFileName(enc_items[index].path) + ", crf=" + crf);
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

        private void OnEncodeCanceled(int index)
        {
            if (is_window_closed_)
            {
                return;
            }
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
                enc_items[index].note = "Canceled";
                enc_items[index].status = EncListItems.Status.cancel;
                ListView1.Items.Refresh();
                Loger.Write(TAG + "OnEncodeCanceled : " + Path.GetFileName(enc_items[index].path) + "\r\n");
            });
        }

        private void OnEncodeFailed(int index, int result_code, string msg)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
                enc_items[index].note = "Fail";
                if (msg.Length > 0)
                {
                    enc_items[index].note = msg;
                }
                enc_items[index].status = EncListItems.Status.fail;
                enc_items[index].progress = 100;
                enc_items[index].progress_color = "Red";
                ListView1.Items.Refresh();
                Loger.Write(TAG + "OnEncodeFailed : " + Path.GetFileName(enc_items[index].path) + ", result_code=" + result_code);
            });
        }

        private void OnEncodeFinished(int index)
        {
            Dispatcher.Invoke(() =>
            {
                List<EncListItems> enc_items = enc_item_manager_.GetEncItems();
                enc_items[index].note = "Success";
                enc_items[index].status = EncListItems.Status.success;
                enc_items[index].progress = 100;
                ListView1.Items.Refresh();
                Loger.Write(TAG + "OnEncodeFinished : " + Path.GetFileName(enc_items[index].path) + "\r\n");
            });
        }

        private void OnAllEncodeFinished()
        {
            Dispatcher.Invoke(() =>
            {
                BtnEncodeCancel.Content = kBtnLabelEncode;
                BtnRemoveDone.IsEnabled = true;
                BtnChangeDestPath.IsEnabled = true;
                Loger.Write(TAG + "OnAllEncodeFinished" + "\r\n");
            });
        }

        #endregion

        private void Window_Activated(object sender, EventArgs e)
        {
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            is_window_closed_ = true;
            enc_manager_.OnWindowClosed();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            bool is_processing = false;
            List<EncListItems> items = new List<EncListItems>();
            foreach (EncListItems item in ListView1.SelectedItems)
            {
                if ( EncListItems.IsStatusProcessing(item.status) )
                {
                    is_processing = true;
                }
                items.Add(item);
            }
            if (is_processing)
            {
                Task.Run(() =>
                {
                    Dispatcher.Invoke(async () =>
                    {
                        Cancel();
                        while (enc_manager_.IsEncoding())
                        {
                            await Task.Delay(1);
                        }
                        enc_item_manager_.RemoveItems(items);
                        ListView1.Items.Refresh();
                        if (enc_item_manager_.GetToEncodeItemsNum() > 0)
                        {
                            Start();
                        }
                    });
                });
            }
            else
            {
                enc_item_manager_.RemoveItems(items);
                ListView1.Items.Refresh();
            }
        }

        private void BtnRemoveDone_Click(object sender, RoutedEventArgs e)
        {
            enc_item_manager_.RemoveFinishedItems();
            ListView1.Items.Refresh();
        }

        private void ComboUsageAll_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            enc_item_manager_.OnCpuUsageChanged((sender as ComboBox).SelectedItem.ToString());
            ListView1.Items.Refresh();
        }
    }
}


