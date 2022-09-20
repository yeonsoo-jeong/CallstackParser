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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using Microsoft.Win32;

namespace ShakaCallstackParser
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string[] textValue = System.IO.File.ReadAllLines(openFileDialog.FileName);
                List<ListItems> result = Parse(textValue);
                ListView1.ItemsSource = result;
                
            }
        }

        private List<ListItems> Parse(string[] inp)
        {
            int startIndex = 0;
            for (int i = 0; i < inp.Length; i++)
            {
                if (inp[i].Length > 0)
                {
                    if (inp[i].Substring(0, 1) == "#")
                    {
                        startIndex = i;
                        break;
                    }
                }
            }

            List<ListItems> output = new List<ListItems>();
            string outTemp = "";
            for (int i = startIndex; i < inp.Length; i++)
            {
                inp[i] = inp[i].Trim();
                if (inp[i].Length > 0)
                {
                    if (inp[i].Substring(0, 1) == "#")
                    {
                        if (outTemp != "")
                        {
                            int sharpIndex = outTemp.IndexOf(" ");
                            string sharpStr = outTemp.Substring(0, sharpIndex);
                            string atStr = "";
                            string inStr = "";
                            int atIndex = outTemp.IndexOf(" at ");
                            if (atIndex >= 0)
                            {
                                atStr = outTemp.Substring(atIndex);
                            }
                            
                            int inIndex = outTemp.IndexOf(" in ");
                            if (inIndex >= 0)
                            {
                                int inSpaceIndex = outTemp.IndexOf(" ", inIndex+4);
                                if (inSpaceIndex >= 0)
                                {  
                                    inStr = outTemp.Substring(inIndex, inSpaceIndex - inIndex);
                                }
                            }

                            if (atStr != "" || inStr != "")
                            {
                                ListItems li = new ListItems();
                                li.numberItem = sharpStr;
                                li.atItem = atStr;
                                li.inItem = inStr;

                                output.Add(li);
                            }
                        }
                        outTemp = inp[i];
                    }
                    else
                    {
                        if (outTemp != "")
                        {
                            outTemp += " ";
                        }

                        outTemp += inp[i];
                    }
                }
            }

            return output;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string[] textValue = TextBox1.Text.Split('\n');
            List<ListItems> result = Parse(textValue);
            ListView1.ItemsSource = result;
        }
    }

    public class ListItems
    {
        public string numberItem { get; set; }
        public string atItem { get; set; }
        public string inItem { get; set; }
    }
}
