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
                if (enc_items_[i].status != EncListItems.Status.success)
                {
                    ret++;
                }
            }

            return ret;
        }

        public List<EncodeJob> GetToEncodeJobs()
        {
            List<EncodeJob> jobs = new List<EncodeJob>();
            for (int i = 0; i < enc_items_.Count; i++)
            {
                if (enc_items_[i].status != EncListItems.Status.success)
                {
                    jobs.Add(new EncodeJob(i, enc_items_[i].path, EncodeManager.GetCoreNumFromCpuUsage(enc_items_[i].cpu_usage_selected)));
                }
            }

            return jobs;
        }

        public void Refresh()
        {
            for (int i = 0; i < enc_items_.Count; i++)
            {
                if (enc_items_[i].status != EncListItems.Status.success)
                {
                    // for cancel & restart scenario
                    enc_items_[i].note = "";
                    enc_items_[i].progress = 0;
                    enc_items_[i].status = EncListItems.Status.none;
                }
            }
        }

        public void DistinctAddFiles(string[] files)
        {
            if (files.Length >= 1)
            {
                foreach (string file in files)
                {
                    EncListItems item = new EncListItems();
                    item.path = file;
                    item.note = "core=" + Environment.ProcessorCount.ToString();
                    item.cpu_usage = new List<string>()
                        {
                            "Full",
                            "Half"
                        };
                    item.cpu_usage_selected = "Full";
                    enc_items_.Add(item);
                }

                enc_items_ = enc_items_.Distinct(new EncListComparer()).ToList();
                ReorderEncListNumber();
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
            success,
            fail
        }

        public EncListItems()
        {
            progress = 0;
            status = Status.none;
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
