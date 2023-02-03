using System;
using System.Collections.Generic;
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
    /// VideoDataViewWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class VideoDataViewWindow : Window
    {
        VDVParser parser_;
        List<PTSListItems> pts_list_ = new List<PTSListItems>();

        public VideoDataViewWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            ListView1.AllowDrop = true;
            ListView1.ItemsSource = pts_list_;
            VDVParser.Callbacks callback = new VDVParser.Callbacks(OnPTSParsed);
            parser_ = new VDVParser(callback);
        }

        private void ListView1_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length >= 1)
                {
                    parser_.Start(files[0]);
                }
            }
        }

        private void OnPTSParsed(string v_pts, string a_pts, string pict_type)
        {
            int number = pts_list_.Count();
            pts_list_.Add(new PTSListItems(number, v_pts, a_pts, pict_type));
            Dispatcher.Invoke(() =>
            {
                ListView1.Items.Refresh();
            });
        }
    }

    public class PTSListItems
    {
        public PTSListItems(int _number, string _v_pts, string _a_pts, string _pict_type)
        {
            number = _number;
            v_pts = _v_pts;
            a_pts = _a_pts;
            pict_type = _pict_type;
        }

        public int number { get; set; }
        public string v_pts { get; set; }
        public string a_pts{ get; set; }
        public string pict_type { get; set; }
    }
}
