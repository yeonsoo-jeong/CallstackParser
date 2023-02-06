using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YsCommon;

namespace ShakaCallstackParser.Model
{
    class EncodeModel : INotifyPropertyChanged
    {
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
