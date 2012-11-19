using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Wrzut
{
    

    public partial class Form1 : Form
    {

        public delegate void UpdateListViewDownStatus(int fileDownPercentage, string fileName);

        WebClient WebC = new WebClient();
        //SelectedLinkTagsArray linksTArray = new SelectedLinkTagsArray();
        //SimpleDelegate delegateLinksAdd;
        CheckedLinksListCollection linksCollection = new CheckedLinksListCollection();
        CheckedLinksListCollection downloadingList = new CheckedLinksListCollection();
        AsyncDownloader downloadWorker;
        public UpdateListViewDownStatus updateListViewStatus = null;

        public Form1()
        {
            InitializeComponent();
            //this.delegateLinksAdd = new SimpleDelegate(linksTArray.Add);
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            string webCode = "";
            try
            {
                webCode = WebC.DownloadString("http://www.wrzuta.pl/szukaj/audio/" + this.searchTextBox.Text.Replace(" ", "+") + "/1").Replace("\n", "");
            }
            catch (WebException exc)
            {
                MessageBox.Show("WebException:\n" + exc.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.AddLinks(webCode);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.downloadWorker = new AsyncDownloader(this);
            listViewDownStatus.ItemAdded += this.downloadWorker.ItemAddedToWorker;
            this.updateListViewStatus = new UpdateListViewDownStatus( this.UpdateFileDownloadedStatus );           
        }

        private void AddLinks(string WebSiteContent)
        {
            this.linksListView.Items.Clear();

            Regex regex1 = new Regex("class=\"file audio\"(.*?)<div class=\"tags\"", RegexOptions.IgnoreCase);
            Regex regex2 = new Regex("class=\"info\">(?<div>.*?)</div", RegexOptions.IgnoreCase);
            Regex regex3 = new Regex("href.*?\"(?<href>.*?)\"", RegexOptions.IgnoreCase);
            foreach (Match match1 in regex1.Matches(WebSiteContent))
            {
                string str1 = match1.Groups[0].Value;
                MatchCollection matchCollection1 = regex3.Matches(str1.Replace("\n", ""));
                MatchCollection matchCollection2 = regex2.Matches(str1.Replace("\n", ""));
                string str2 = "";
                string text1 = "";
                string text2 = "";
                foreach (Match match2 in matchCollection1)
                {
                    string str3 = match2.Groups["href"].Value;
                    if (str3.Contains("audio"))
                        str2 = str3;
                }
                foreach (Match match2 in matchCollection2)
                {
                    string str3 = match2.Groups["div"].Value;
                    text1 = str3.Replace("<b>", "").Replace("</b>", "").Replace(" ", "").Split("|".ToCharArray())[1];
                    text1 = text1.Replace("<spanclass=\"size\">", "").Replace("</span>", "");
                    text2 = str3.Replace("<b>", "").Replace("</b>", "").Replace(" ", "").Split("|".ToCharArray())[0].Replace("<spanclass=\"duration\">", "").Replace("</span>", "");
                }
                if (str2 != "" && !str2.Contains("najnowsze"))
                {
                    ListViewItem listViewItem = this.linksListView.Items.Add(str2.Remove(0, str2.LastIndexOf("/") + 1).Replace("_", " "));
                    listViewItem.SubItems.Add(text2);
                    listViewItem.SubItems.Add(text1);
                    listViewItem.SubItems.Add("Not started");
                    Random random = new Random();
                    string str3 = str2.Replace("/audio/", "/xml/plik/");
                    string str4 = str3.Remove(str3.LastIndexOf("/"));
                    listViewItem.Tag = (object)(str4 + "/wrzuta.pl/sa/" + random.Next(100000).ToString());
                }
            }
        }

        private void downloadButton_Click(object sender, EventArgs e)
        {
            ListView.CheckedListViewItemCollection checkedItems = this.linksListView.CheckedItems;
            if (checkedItems.Count == 0)
            {
                MessageBox.Show("Brak wybranych utworów!!!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                //TODO: Download worker
                //Adding to worker
                foreach(ListViewItem item  in checkedItems)
                {
                    this.linksCollection.Add(item.Tag);
                }

                processAllXMLFiles();
            }
        }

        private bool processAllXMLFiles()
        {
            WebClient webXMLClient = new WebClient();

            try
            {
                foreach(string key in new List<string>(this.linksCollection.Dict.Keys))
                {
                    string xmlFileInfo = webXMLClient.DownloadString(key).Replace("\n", "");
                    string fileName = xmlFileInfo.Remove(0, xmlFileInfo.IndexOf("<name>") + 6);
                    fileName = fileName.Remove(fileName.IndexOf("</name>")).Replace("<![CDATA[", "").Replace("]]>", "");


                    xmlFileInfo = xmlFileInfo.Remove(0, xmlFileInfo.IndexOf("<fileId>") + 8);
                    xmlFileInfo = xmlFileInfo.Remove(xmlFileInfo.IndexOf("</fileId>")).Replace("<![CDATA[", "").Replace("]]>", "");
                    this.linksCollection.Dict[key] = xmlFileInfo;

                    ListViewItem item = new ListViewItem(fileName);
                    item.Tag = xmlFileInfo;
                    item.SubItems.Add((new ListViewItem.ListViewSubItem()).Text = "0 %");
                    item.SubItems.Add((new ListViewItem.ListViewSubItem()).Text = "NONE");
                    this.listViewDownStatus.AddItem(item);
                    
                }
            }
            catch (WebException exc)
            {
                return false;
            }

            return true;
        }

        private void UpdateFileDownloadedStatus(int percentage, string fileName)
        {
            System.Windows.Forms.ListView.ListViewItemCollection Items = this.listViewDownStatus.Items;
            int itemsNo = Items.Count;

            for (int i = 0; i < itemsNo; i++)
            {
                if (fileName == Items[i].Text)
                {
                    Items[i].SubItems[1].Text = percentage.ToString() + "%";
                }
                else
                {
                    Items[i].SubItems[1].Text = "0 %";
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            downloadWorker.TerminateThread();    
        }


    }
}
