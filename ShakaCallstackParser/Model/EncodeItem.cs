using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShakaCallstackParser.Model
{
    class EncodeItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public EncodeItem()
        {
            progress = 0;
            encode_status = Status.none;
            note = "";
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
}
