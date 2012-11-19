using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Windows.Forms;

namespace Wrzut
{
    class AsyncDownloader
    {
        private WebClient downloadWebClient = new WebClient();

        private List<ListViewItem> itemsInQueue = new List<ListViewItem>();
        private bool downloadInProgress = false;
        private Thread downloadThread = null;
        private bool thStarted = false;
        private Form1 FormCtrl = null;
        private string currentDownloadFileText = null;

        public AsyncDownloader(Form1 FormCtrl)
        {
            this.FormCtrl = FormCtrl;

            //Register events
            // Specify that the DownloadFileCallback method gets called // when the download completes.
            downloadWebClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCallback);
            // Specify a progress notification handler.
            downloadWebClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
        }

        public void ItemAddedToWorker(object oSender, EventArgs oEventArgs)
        {
            Monitor.Enter(this.itemsInQueue);
            this.itemsInQueue.Add((System.Windows.Forms.ListViewItem)((ItemsAddedArgs)oEventArgs).Item);
            Monitor.Exit(this.itemsInQueue);
            if (!thStarted)
            {
                this.downloadThread = new Thread(new ThreadStart(this.StartNewAsyncDownloadThread));
                this.downloadThread.Start();
            }
        }

        private void StartNewAsyncDownloadThread()
        {
            this.thStarted = true;  //this method is executed once by adding first file to queue

            while (true)
            {
                //there is no active jobs in queue. Stop the thread
                if (this.itemsInQueue.Count == 0)
                {
                    this.thStarted = false;
                    this.downloadInProgress = false;
                    break;
                }

                //In case there is pending download do not invoke another one!
                if(this.downloadInProgress)
                {
                    Thread.Sleep(2000);
                    continue;
                }

                ListViewItem Item = null;
                Monitor.Enter(this.itemsInQueue);
                    Item = this.itemsInQueue.First<ListViewItem>(); //get the first item from queue
                Monitor.Exit(this.itemsInQueue);

                this.downloadWebClient.DownloadFileAsync(new Uri(Item.Tag.ToString()), GetDirectoryPath() + Item.Text + ".mp3", (object)Item.Tag);
                this.currentDownloadFileText = Item.Text;
                this.downloadInProgress = true;
                Thread.Sleep(2000); //in case everything is downloaded
            }
        }

        public void DownloadFileCallback(object Sender, EventArgs args)
        {
            Monitor.Enter(this.itemsInQueue);
                this.itemsInQueue.RemoveAt(0); //remove first element
                this.downloadInProgress = false;
            Monitor.Exit(this.itemsInQueue);
        }

        public void DownloadProgressCallback(object Sender, EventArgs args)
        {
            double bytesReceived = (double) ((DownloadProgressChangedEventArgs) args).BytesReceived;
            double bytesTotal = ((DownloadProgressChangedEventArgs)args).TotalBytesToReceive;
            int totalProgressPerc = (int)(bytesReceived / bytesTotal * 100.0);

            FormCtrl.Invoke(FormCtrl.updateListViewStatus, new object[] {totalProgressPerc, this.currentDownloadFileText});
            return;   
        }

        private string GetDirectoryPath()
        {
            String filePath = new String(Wrzut.Properties.Settings.Default.DownloadPath.ToCharArray());
            Regex envRegex = new Regex("(%[a-zA-Z0-9]*%)", RegexOptions.IgnoreCase);

            int matches = envRegex.Matches(Wrzut.Properties.Settings.Default.DownloadPath).Count;
            for (int i = 0; i < matches; i++)
            {
                string variable = Environment.ExpandEnvironmentVariables(envRegex.Matches(Wrzut.Properties.Settings.Default.DownloadPath)[i].Value);
                filePath = filePath.Replace(envRegex.Matches(Wrzut.Properties.Settings.Default.DownloadPath)[i].Value, variable);
            }

            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            return filePath = filePath + "\\";
        }

        public void TerminateThread()
        {
            if (this.downloadThread != null)
            {
                this.downloadThread.Abort();
            }
        }


    }
}
