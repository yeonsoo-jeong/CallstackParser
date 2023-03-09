using ShakaCallstackParser.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ShakaCallstackParser
{
    class EncItemManager
    {
        ObservableCollection<EncodeItem> enc_items_;

        long id_ = 0;

        public EncItemManager(ObservableCollection<EncodeItem> items)
        {
            enc_items_ = items;
        }

        public ObservableCollection<EncodeItem> GetEncItems()
        {
            return enc_items_;
        }

        // To be applied
        public int GetIndexById(int id)
        {
            string id_str = id.ToString();
            for (int i = 0; i < enc_items_.Count; i++)
            {
                if (enc_items_[i].Id == id_str)
                {
                    return i;
                }
            }

            return -1;
        }

        public void RemoveItems(List<EncodeItem> items)
        {
            foreach (EncodeItem item in items)
            {
                enc_items_.Remove(item);
            }
            ReorderEncListNumber();
        }

        public void RemoveFinishedItems()
        {
            for (int i = enc_items_.Count - 1; i >= 0; i--)
            {
                if (enc_items_[i].EncodeStatus == EncodeItem.Status.success)
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
                if (EncodeItem.IsStatusShouldEncode(enc_items_[i].EncodeStatus) )
                {
                    ret++;
                }
            }

            return ret;
        }

        public EncodeItem GetToEncodeFirstItem()
        {
            for (int i = 0; i < enc_items_.Count; i++)
            {
                if (EncodeItem.IsStatusShouldEncode(enc_items_[i].EncodeStatus))
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
                if (EncodeItem.IsStatusShouldEncode(enc_items_[i].EncodeStatus) )
                {
                    // for cancel & restart scenario
                    enc_items_[i].Note = "";
                    enc_items_[i].Progress = 0;
                    enc_items_[i].EncodeStatus = EncodeItem.Status.none;
                }
            }
        }

        public void DistinctAddFiles(string[] files, string cpu_usage)
        {
            if (files.Length >= 1)
            {
                foreach (string file in files)
                {
                    EncodeItem item = new EncodeItem();
                    item.Id = id_.ToString();
                    id_++;
                    item.Path = file;
                    item.CpuUsage = new List<string>(EncWindow.kCpuUsageItems);
                    item.CpuUsageSelected = cpu_usage;
                    item.ProgressColor = "LimeGreen";
                    enc_items_.Add(item);
                }

                List<EncodeItem> temp = enc_items_.Distinct(new EncListComparer()).ToList();
                enc_items_.Clear();
                temp.ForEach(x => enc_items_.Add(x));
                ReorderEncListNumber();
            }
        }

        public void OnCpuUsageChanged(string changed_item)
        {
            for (int i = 0; i < enc_items_.Count; i++)
            {
                if (EncodeItem.IsStatusShouldEncode(enc_items_[i].EncodeStatus) )
                {
                    enc_items_[i].CpuUsageSelected = changed_item;
                }
            }
        }

        private void ReorderEncListNumber()
        {
            for (int i = 0; i < enc_items_.Count; i++)
            {
                enc_items_[i].Number = i.ToString();
            }
        }
    }

    public class EncListComparer : IEqualityComparer<EncodeItem>
    {
        //public bool Equals(EncodeItem x, EncodeItem y)
        bool IEqualityComparer<EncodeItem>.Equals(EncodeItem x, EncodeItem y)
        {
            return x.Path == y.Path;
        }

        int IEqualityComparer<EncodeItem>.GetHashCode(EncodeItem obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return obj.Path == null ? 0 : obj.Path.GetHashCode();
        }
    }
}
