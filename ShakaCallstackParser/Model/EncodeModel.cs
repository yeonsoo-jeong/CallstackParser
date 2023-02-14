using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YsCommon;

namespace ShakaCallstackParser.Model
{
    class EncodeModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public const string kBtnLabelEncode = "Encode";
        public const string kBtnLabelCancel = "Cancel";

        private string dest_path_ = ConfigManager.GetDestPath();
        public string DestPath
        {
            get
            {
                return dest_path_;
            }
            set
            {
                dest_path_ = value;
                OnPropertyChanged("DestPath");
            }
        }

        private string cpu_usage_item_selected_ = EncWindow.kCpuUsageItems[0];
        public string CpuUsageItemSelected
        {
            get
            {
                return cpu_usage_item_selected_;
            }
            set
            {
                cpu_usage_item_selected_ = value;
                OnPropertyChanged("CpuUsageItemSelected");
            }
        }

        private string btn_enc_cancel_string = kBtnLabelEncode;
        public string BtnEncCancelString
        {
            get { return btn_enc_cancel_string; }
            set
            {
                btn_enc_cancel_string = value;
                OnPropertyChanged("BtnEncCancelString");
            }
        }

        private bool btn_enc_cancel_enabled = true;
        public bool BtnEncCancelEnabled
        {
            get { return btn_enc_cancel_enabled; }
            set
            {
                btn_enc_cancel_enabled = value;
                OnPropertyChanged("BtnEncCancelEnabled");
            }
        }


        private bool btn_remove_done_enabled = true;
        public bool BtnRemoveDoneEnabled
        {
            get { return btn_remove_done_enabled; }
            set
            {
                btn_remove_done_enabled = value;
                OnPropertyChanged("BtnRemoveDoneEnabled");
            }
        }


        private bool btn_change_dest_path_enabled = true;
        public bool BtnChangeDestPathEnabled
        {
            get { return btn_change_dest_path_enabled; }
            set
            {
                btn_change_dest_path_enabled = value;
                OnPropertyChanged("BtnChangeDestPathEnabled");
            }
        }
    }
}
