using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YsCommon;

namespace ShakaCallstackParser.ViewModel
{
    class EncodeViewModel : INotifyPropertyChanged
    {
        private Model.EncodeModel model = null;
        public Command cmd_browse_dest_path { get; set; }

        public EncodeViewModel()
        {
            model = new Model.EncodeModel();
            cmd_browse_dest_path = new Command(Execute_BrowseDestPath, CanExecute_BrowseDestPath);
        }

        public Model.EncodeModel Model
        {
            get { return model; }
            set
            {
                model = value;
                OnPropertyChanged("Model");
            }
        }

        private void Execute_BrowseDestPath(object obj)
        {
            var dlg = new FolderPicker();
            //dlg.InputPath = @"c:\windows\system32";
            if (dlg.ShowDialog() == true)
            {
                model.DestPath = dlg.ResultPath;
                ConfigManager.SetDestPath(dlg.ResultPath);
            }
        }

        private bool CanExecute_BrowseDestPath(object obj)
        {
            return true;
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
