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
using System.Collections.ObjectModel;

using YsCommon;
using ShakaCallstackParser.Model;

namespace ShakaCallstackParser
{
    /// <summary>
    /// EncWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EncWindow : Window
    {
        ViewModel.EncodeViewModel view_model_;

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

        private void Init()
        {
            view_model_ = new ViewModel.EncodeViewModel();
            this.DataContext = view_model_;

            view_model_.Init();
        }

        private void ListView1_OnDroped(object sender, DragEventArgs e)
        {
            view_model_.ListView_OnDroped(sender, e);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            view_model_.Window_Closed();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            view_model_.MenuItem_Click();
        }

        private void ComboUsageAll_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            view_model_.ComboUsageAll_SelectionChanged();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            view_model_.SelectedListviewItems = ListView1.SelectedItems.Cast<EncodeItem>().ToList();
        }
    }
}


