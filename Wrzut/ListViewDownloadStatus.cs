using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Wrzut
{
    class ListViewDownloadStatus : ListView
    {
        public EventHandler<ItemsAddedArgs> ItemAdded;

        public void AddItem(ListViewItem item)
        {
            Items.Add(item);
            if (ItemAdded != null)
                ItemAdded.Invoke(this, new ItemsAddedArgs(item));
        }
    }

    public class ItemsAddedArgs : EventArgs
    {
        public ItemsAddedArgs(ListViewItem item)
        {
            Item = item;
        }

        public object Item { get; set; }
    }
}
