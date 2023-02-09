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

        private string cpu_usage_item_selected_ = "Full";
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

        public class EncodeItem : INotifyPropertyChanged
        {
            public EncodeItem()
            {
                progress = 0;
                encode_status = Status.none;
                note = "";
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }

            public static bool IsStatusShouldEncode(Status status)
            {
                if (status == Status.none || status == Status.cancel)
                {
                    return true;
                }
                return false;
            }

            public static bool IsStatusProcessing(Status status)
            {
                if (status == Status.analyzing || status == Status.encoding)
                {
                    return true;
                }
                return false;
            }

            private string number;
            public string Number
            {
                get { return number; }
                set
                {
                    number = value;
                    OnPropertyChanged("Number");
                }
            }

            private string path;
            public string Path
            {
                get { return path; }
                set
                {
                    path = value;
                    OnPropertyChanged("Path");
                }
            }

            private int progress;
            public int Progress
            {
                get { return progress; }
                set
                {
                    progress = value;
                    OnPropertyChanged("Progress");
                }
            }

            private string progress_color;
            public string ProgressColor
            {
                get { return progress_color; }
                set
                {
                    progress_color = value;
                    OnPropertyChanged("ProgressColor");
                }
            }

            private List<string> cpu_usage;
            public List<string> CpuUsage
            {
                get { return cpu_usage; }
                set
                {
                    cpu_usage = value;
                    OnPropertyChanged("CpuUsage");
                }
            }

            private string cpu_usage_selected;
            public string CpuUsageSelected
            {
                get { return cpu_usage_selected; }
                set
                {
                    cpu_usage_selected = value;
                    OnPropertyChanged("CpuUsageSelected");
                }
            }

            private string note;
            public string Note
            {
                get { return note; }
                set
                {
                    note = value;
                    OnPropertyChanged("Note");
                }
            }


            private Status encode_status;
            public Status EncodeStatus
            {
                get { return encode_status; }
                set
                {
                    encode_status = value;
                    OnPropertyChanged("EncodeStatus");
                }
            }

            public enum Status
            {
                none,
                analyzing,
                encoding,
                success,
                cancel,
                fail
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
