using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShakaCallstackParser
{
    class EncItemManager
    {
        List<EncListItems> enc_items_ = new List<EncListItems>();

        public EncItemManager()
        {

        }

        public List<EncListItems> GetEncItems()
        {
            return enc_items_;
        }

        public int GetIndexByNumber(int number)
        {
            string num_str = number.ToString();
            for (int i = 0; i < enc_items_.Count; i++)
            {
                if (enc_items_[i].number == num_str)
                {
                    return i;
                }
            }

            return -1;
        }

        public void RemoveItems(List<EncListItems> items)
        {
            foreach (EncListItems item in items)
            {
                enc_items_.Remove(item);
            }
            ReorderEncListNumber();
        }

        public void RemoveFinishedItems()
        {
            for (int i = enc_items_.Count - 1; i >= 0; i--)
            {
                if (enc_items_[i].status == EncListItems.Status.success)
                {
                    enc_items_.RemoveAt(i);
                }
            }
            ReorderEncListNumber();
        }

        public int GetToEncodeItemsNum()
        {
            int ret = 0;
            for (int i = 0; i < enc_items_.Count; i++)
            {
                if ( EncListItems.IsStatusShouldEncode(enc_items_[i].status) )
                {
                    ret++;
                }
            }

            return ret;
        }

        public EncListItems GetToEncodeFirstItem()
        {
            for (int i = 0; i < enc_items_.Count; i++)
            {
                if (EncListItems.IsStatusShouldEncode(enc_items_[i].status))
                {
                    return enc_items_[i];
                }
            }

            return null;
        }

        public void Refresh()
        {
            for (int i = 0; i < enc_items_.Count; i++)
            {
                if ( EncListItems.IsStatusShouldEncode(enc_items_[i].status) )
                {
                    // for cancel & restart scenario
                    enc_items_[i].note = "";
                    enc_items_[i].progress = 0;
                    enc_items_[i].status = EncListItems.Status.none;
                }
            }
        }

        public void DistinctAddFiles(string[] files, string cpu_usage)
        {
            if (files.Length >= 1)
            {
                foreach (string file in files)
                {
                    EncListItems item = new EncListItems();
                    item.path = file;
                    item.cpu_usage = new List<string>(EncWindow.kCpuUsageItems);
                    item.cpu_usage_selected = cpu_usage;
                    enc_items_.Add(item);
                }

                enc_items_ = enc_items_.Distinct(new EncListComparer()).ToList();
                ReorderEncListNumber();
            }
        }

        public void OnCpuUsageChanged(string changed_item)
        {
            for (int i = 0; i < enc_items_.Count; i++)
            {
                if ( EncListItems.IsStatusShouldEncode(enc_items_[i].status) )
                {
                    enc_items_[i].cpu_usage_selected = changed_item;
                }
            }
        }

        private void ReorderEncListNumber()
        {
            for (int i = 0; i < enc_items_.Count; i++)
            {
                enc_items_[i].number = i.ToString();
            }
        }
    }

    public class EncListItems
    {
        public enum Status
        {
            none,
            analyzing,
            encoding,
            success,
            fail
        }

        public EncListItems()
        {
            progress = 0;
            status = Status.none;
            note = "";
        }

        public static bool IsStatusShouldEncode(Status status)
        {
            if (status == Status.none || status == Status.fail)
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

        public string number { get; set; }
        public string path { get; set; }
        public int progress { get; set; }

        public List<string> cpu_usage { get; set; }

        public string cpu_usage_selected { get; set; }

        public string note { get; set; }
        public Status status { get; set; }
    }

    public class EncListComparer : IEqualityComparer<EncListItems>
    {
        public bool Equals(EncListItems x, EncListItems y)
        {
            return x.path == y.path;
        }

        public int GetHashCode(EncListItems obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return obj.path == null ? 0 : obj.path.GetHashCode();
        }
    }
}
