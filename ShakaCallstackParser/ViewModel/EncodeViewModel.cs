using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using YsCommon;

namespace ShakaCallstackParser.ViewModel
{
    class EncodeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        const string TAG = "EncodeViewModel.cs : ";

        private Model.EncodeModel encode_model = null;
        public Command cmd_browse_dest_path { get; set; }
        public Command cmd_open_saved_folder { get; set; }
        public Command cmd_encode_cancel { get; set; }
        public Command cmd_remove_done { get; set; }

        public ObservableCollection<Model.EncodeItem> EncodeItemList { get; set; }
        public List<Model.EncodeItem> SelectedListviewItems { get; set; }

        public ObservableCollection<string> CpuUsageItems { get; set; }

        EncodeManager enc_manager_;
        EncItemManager enc_item_manager_;
        bool is_window_closed_ = false;

        public EncodeViewModel()
        {
            encode_model = new Model.EncodeModel();
            cmd_browse_dest_path = new Command(Execute_BrowseDestPath, CanExecute_BrowseDestPath);
            cmd_open_saved_folder = new Command(Execute_OpenSavedFolder, CanExecute_OpenSavedFolder);
            cmd_encode_cancel = new Command(Execute_EncodeCancel, CanExecute_EncodeCancel);
            cmd_remove_done = new Command(Execute_RemoveDone, CanExecute_RemoveDone);

            EncodeItemList = new ObservableCollection<Model.EncodeItem>()
            {
            };

            SelectedListviewItems = new List<Model.EncodeItem>(); // no need??

            CpuUsageItems = new ObservableCollection<string>(EncWindow.kCpuUsageItems.ToArray());
        }

        public Model.EncodeModel EncModel
        {
            get { return encode_model; }
            set
            {
                encode_model = value;
                OnPropertyChanged("Model");
            }
        }

        private void Execute_BrowseDestPath(object obj)
        {
            var dlg = new FolderPicker();
            //dlg.InputPath = @"c:\windows\system32";
            if (dlg.ShowDialog() == true)
            {
                encode_model.DestPath = dlg.ResultPath;
                ConfigManager.SetDestPath(dlg.ResultPath);
            }
        }

        private bool CanExecute_BrowseDestPath(object obj)
        {
            return true;
        }

        private void Execute_OpenSavedFolder(object obj)
        {
            Process.Start(encode_model.DestPath);
        }

        private bool CanExecute_OpenSavedFolder(object obj)
        {
            return true;
        }

        private void Execute_EncodeCancel(object obj)
        {
            if (EncModel.BtnEncCancelString == Model.EncodeModel.kBtnLabelEncode)
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
            else if (EncModel.BtnEncCancelString == Model.EncodeModel.kBtnLabelCancel)
            {
                Cancel();
            }
            else
            {
                // Unexpected Scenario
                Loger.Write("EncWindow.xaml.cs : BtnEncodeCancel_Click : Unexpected Scenario");
            }
        }

        private bool CanExecute_EncodeCancel(object obj)
        {
            return true;
        }

        private void Execute_RemoveDone(object obj)
        {
            enc_item_manager_.RemoveFinishedItems();
        }

        private bool CanExecute_RemoveDone(object obj)
        {
            return true;
        }

        public void Init()
        {
            EncodeManager.Callbacks callback = new EncodeManager.Callbacks(OnEncodeStarted,
                OnEncodeProgressChanged, OnEncodeCanceled, OnEncodeFailed, OnEncodeFinished, OnAllEncodeFinished,
                OnAnalyzeStarted, OnAnalyzeCanceled, OnAnalyzeFailed, OnAnalyzeFinished);

            enc_item_manager_ = new EncItemManager(EncodeItemList);
            enc_manager_ = new EncodeManager(callback, enc_item_manager_);

            EncodingFileManager.DeleteAllTempFiles();
        }

        public void ListView_OnDroped(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length >= 1)
                {
                    enc_item_manager_.DistinctAddFiles(files, EncModel.CpuUsageItemSelected);
                }
            }
        }

        public void Start()
        {
            EncModel.BtnEncCancelEnabled = false;

            enc_item_manager_.Refresh();

            string dest_path = EncModel.DestPath;
            Task.Run(() => enc_manager_.Start(dest_path));

            EncModel.BtnEncCancelString = Model.EncodeModel.kBtnLabelCancel;
            EncModel.BtnRemoveDoneEnabled = false;
            EncModel.BtnChangeDestPathEnabled = false;

            Task.Delay(1000).ContinueWith(_ =>
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    EncModel.BtnEncCancelEnabled = true;
                });
            });
        }

        public void Cancel()
        {
            EncModel.BtnEncCancelEnabled = false;

            enc_manager_.OnEncodeCanceled();
            EncModel.BtnEncCancelString = Model.EncodeModel.kBtnLabelEncode;
            EncModel.BtnRemoveDoneEnabled = true;
            EncModel.BtnChangeDestPathEnabled = true;

            Task.Delay(1000).ContinueWith(_ =>
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    EncModel.BtnEncCancelEnabled = true;
                });
            });
        }

        #region Analyzer Callback
        private void OnAnalyzeStarted(int index)
        {
            EncodeItemList[index].Note = "Analyzing";
            EncodeItemList[index].EncodeStatus = Model.EncodeItem.Status.analyzing;
            Loger.Write(TAG + "OnAnalyzeStarted : " + Path.GetFileName(EncodeItemList[index].Path));
        }

        private void OnAnalyzeCanceled(int index)
        {
            EncodeItemList[index].Note = "Canceled";
            EncodeItemList[index].EncodeStatus = Model.EncodeItem.Status.cancel;
            Loger.Write(TAG + "OnAnalyzeCanceled : " + Path.GetFileName(EncodeItemList[index].Path) + "\r\n");
        }

        private void OnAnalyzeFailed(int index, string msg)
        {
            EncodeItemList[index].Note = "Analyzation Failed";
            if (msg.Length > 0)
            {
                EncodeItemList[index].Note = msg;
            }
            EncodeItemList[index].Progress = 100;
            EncodeItemList[index].ProgressColor = "Red";
            EncodeItemList[index].EncodeStatus = Model.EncodeItem.Status.fail;
            Loger.Write(TAG + "OnAnalyzeFailed : " + Path.GetFileName(EncodeItemList[index].Path) + "\r\n");
        }

        private void OnAnalyzeFinished(int index, int crf)
        {
            Loger.Write(TAG + "OnAnalyzeFinished : " + Path.GetFileName(EncodeItemList[index].Path) + ", crf=" + crf);
        }
        #endregion

        #region Encoder Callback
        private void OnEncodeStarted(int index, int crf)
        {
            EncodeItemList[index].Note = "Encoding";
            EncodeItemList[index].EncodeStatus = Model.EncodeItem.Status.encoding;
            Loger.Write(TAG + "OnEncodeStarted : " + Path.GetFileName(EncodeItemList[index].Path) + ", crf=" + crf);
        }

        private void OnEncodeProgressChanged(int index, int percentage)
        {
            EncodeItemList[index].Progress = percentage;
        }

        private void OnEncodeCanceled(int index)
        {
            if (is_window_closed_)
            {
                return;
            }

            EncodeItemList[index].Note = "Canceled";
            EncodeItemList[index].EncodeStatus = Model.EncodeItem.Status.cancel;
            Loger.Write(TAG + "OnEncodeCanceled : " + Path.GetFileName(EncodeItemList[index].Path) + "\r\n");
        }

        private void OnEncodeFailed(int index, int result_code, string msg)
        {
            EncodeItemList[index].Note = "Fail";
            if (msg.Length > 0)
            {
                EncodeItemList[index].Note = msg;
            }
            EncodeItemList[index].EncodeStatus = Model.EncodeItem.Status.fail;
            EncodeItemList[index].Progress = 100;
            EncodeItemList[index].ProgressColor = "Red";
            Loger.Write(TAG + "OnEncodeFailed : " + Path.GetFileName(EncodeItemList[index].Path) + ", result_code=" + result_code);
        }

        private void OnEncodeFinished(int index)
        {
            EncodeItemList[index].Note = "Success";
            EncodeItemList[index].EncodeStatus = Model.EncodeItem.Status.success;
            EncodeItemList[index].Progress = 100;
            Loger.Write(TAG + "OnEncodeFinished : " + Path.GetFileName(EncodeItemList[index].Path) + "\r\n");
        }

        private void OnAllEncodeFinished()
        {
            EncModel.BtnEncCancelString = Model.EncodeModel.kBtnLabelEncode;
            EncModel.BtnRemoveDoneEnabled = true;
            EncModel.BtnChangeDestPathEnabled = true;
            Loger.Write(TAG + "OnAllEncodeFinished" + "\r\n");
        }
        #endregion

        public void Window_Closed()
        {
            is_window_closed_ = true;
            enc_manager_.OnWindowClosed();
        }

        public void MenuItem_Click()
        {
            bool is_processing = false;
            List<Model.EncodeItem> items = new List<Model.EncodeItem>();
            foreach (Model.EncodeItem item in SelectedListviewItems)
            {
                if (Model.EncodeItem.IsStatusProcessing(item.EncodeStatus))
                {
                    is_processing = true;
                }
                items.Add(item);
            }
            if (is_processing)
            {
                Task.Run(() =>
                {
                    Dispatcher.CurrentDispatcher.Invoke(async () =>
                    {
                        Cancel();
                        while (enc_manager_.IsEncoding())
                        {
                            await Task.Delay(1);
                        }
                        enc_item_manager_.RemoveItems(items);
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
            }
        }

        public void ComboUsageAll_SelectionChanged()
        {
            enc_item_manager_.OnCpuUsageChanged(EncModel.CpuUsageItemSelected);
        }
    }
}
