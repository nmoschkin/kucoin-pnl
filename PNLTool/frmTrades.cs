using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

using System.Threading;
using System.Reflection;

using DataTools.Text.Csv;

namespace PNLTool
{
    public partial class frmTrades : Form
    {
        public static readonly Guid ProjectId = Guid.Parse("{8BC88886-22CB-4709-9038-688937626370}");

        CsvWrapper dataFile;
        CsvWrapper koinly;

        private List<string> openFiles = new List<string>();

        private List<TradeEntry> trades = new List<TradeEntry>();

        public frmTrades()
        {

            InitializeComponent();

            this.Location = Settings.LastWindowLocation;
            this.ClientSize = Settings.LastWindowSize;

            this.Load += FrmTrades_Load;

            mnuClear.Click += MnuClear_Click;
            mnuExit.Click += MnuExit_Click;
            mnuRemoveItem.Click += MnuRemoveItem_Click;

            UpdateRecentFiles();

        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            Settings.LastWindowLocation = Location;
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Settings.LastWindowSize = ClientSize;
        }

        private void FrmTrades_Load(object sender, EventArgs e)
        {
            this.Location = Settings.LastWindowLocation;
            this.Size = Settings.LastWindowSize;

            var afiles = Settings.ActiveFiles;

            foreach (var file in afiles)
            {
                FileToList(file, lvOut);
            }

            EnableButtons();
        }

        private void MnuRemoveItem_Click(object sender, EventArgs e)
        {
            var c = lvOut.SelectedItems?.Count ?? 0;

            if (c == 0) return;

            var items = new List<ListViewItem>();
            foreach (ListViewItem sel in lvOut.SelectedItems)
            {
                items.Add(sel);
            }

            var act = new List<string>(Settings.ActiveFiles);

            foreach (ListViewItem lvi in items)
            {
                lvOut.Items.Remove(lvi);
                var tag = lvi.Tag as string;

                if (!string.IsNullOrEmpty(tag))
                {
                    openFiles.Remove(tag);

                    if (act.Contains(tag))
                    {
                        act.Remove(tag);
                    }

                }
            }

            Settings.ActiveFiles = act.ToArray();
            EnableButtons();
        }

        private void MnuExit_Click(object sender, EventArgs e)
        { 
            Application.Exit();
        }

        private void MnuClear_Click(object sender, EventArgs e)
        {
            btnClear_Click(sender, e);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Title = "Browser For CSV File",
                Filter = "Comma Separated Values Files (*.csv)|*.csv|Any File (*.*)|*.*",
                InitialDirectory = Settings.LastDirectory,
                Multiselect = true
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (openFiles.Contains(dlg.FileName))
                {
                    MessageBox.Show("File is already open!");
                    return;
                }

                var selFiles = dlg.FileNames;
                txtFilename.Text = dlg.FileName;

                foreach (var file in selFiles)
                {
                    FileToList(file, lvOut);

                    Settings.AddRecentFile(file, ProjectId);

                    var act = new List<string>(Settings.ActiveFiles);
                    if (!act.Contains(file))
                    {
                        act.Add(file);
                        Settings.ActiveFiles = act.ToArray();
                    }

                }

                UpdateRecentFiles();

            }
        }

        private void UpdateRecentFiles()
        {
            foreach (var mnuObject in mnuRecent.DropDownItems)
            {
                if (mnuObject is ToolStripMenuItem oldItem)
                {
                    oldItem.Click -= MnuItem_Click;
                }
            }

            mnuRecent.DropDownItems.Clear();

            var recents = Settings.FilterRecentList(ProjectId, ".csv");

            foreach (var recent in recents)
            {
                var mnuItem = mnuRecent.DropDownItems.Add(Path.GetFileName(recent.FileName));

                mnuItem.ToolTipText = recent.FileName;
                mnuItem.Click += MnuItem_Click;

            }

        }

        private void MnuItem_Click(object sender, EventArgs e)
        {
            var mnuItem = sender as ToolStripMenuItem;

            txtFilename.Text = mnuItem.ToolTipText;

            FileToList(txtFilename.Text, lvOut);

            Settings.AddRecentFile(txtFilename.Text, ProjectId);
            UpdateRecentFiles();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtFilename.Text = "";

            lvData.Items.Clear();
            lvData.Columns.Clear();

            lvOut.Items.Clear();
            lvOut.Columns.Clear();

            openFiles.Clear();
            trades.Clear();

            dataFile = null;
            koinly = null;

            EnableButtons();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            dataFile = new CsvWrapper();

            lvData.Items.Clear();
            lvData.Columns.Clear();

            foreach (var file in openFiles)
            {
                if (!dataFile.OpenDocument(file)) return;
                CsvToList(dataFile, lvData);
            }

        }

        private void EnableButtons()
        {
            btnStart.Enabled = lvOut.Items.Count > 0;
            btnConv.Enabled = (dataFile != null);
            btnCalc.Enabled = (dataFile != null);
            btnExport.Enabled = (koinly != null);
        }

        private void FileToList(string fileName, ListView listView, bool clear = false)
        {
            if (clear || listView.Columns.Count != 4)
            {
                listView.Items.Clear();
                listView.Columns.Clear();

                listView.Columns.Add("Filename").Width = 200;   
                listView.Columns.Add("Size").Width = 60;
                listView.Columns.Add("Last Modified Date").Width = 150;
                listView.Columns.Add("Folder").Width = 200;
            }

            if (openFiles.Contains(fileName)) return;
            openFiles.Add(fileName);

            if (File.Exists(fileName))
            {
                var lvi = listView.Items.Add(Path.GetFileName(fileName));

                lvi.Tag = lvi.ToolTipText = fileName;

                using(var fo = File.OpenRead(fileName))
                {
                    lvi.SubItems.Add(fo.Length.ToString());
                }

                lvi.SubItems.Add(File.GetLastWriteTime(fileName).ToString("G"));
                lvi.SubItems.Add(Path.GetDirectoryName(fileName));
            }
        }

        private void CsvToList(CsvWrapper csv, ListView listView, bool clear = false) 
        { 

            if (clear || listView.Columns.Count != csv.ColumnNames.Count())
            {
                listView.Items.Clear();
                listView.Columns.Clear();

                foreach (var c in csv.ColumnNames)
                {
                    listView.Columns.Add(new ColumnHeader()
                    {
                        Width = 120,
                        Text = c
                    });
                }
            }

            btnConv.Enabled = true;

            foreach (var r in csv)
            {
                var strs = r.GetValues();

                var lvRow = new ListViewItem(strs.FirstOrDefault());

                int c = strs.Length;


                for (int i = 1; i < c; i++)
                {
                    lvRow.SubItems.Add(strs[i]);
                }

                listView.Items.Add(lvRow);
            }

            EnableButtons();
        }

        private void btnConv_Click(object sender, EventArgs e)
        {

            koinly = new CsvWrapper();

            koinly.ColumnNames = "Koinly Date,Pair,Side,Amount,Total,Fee Amount,Fee Currency,Order ID,Trade ID";

            var cnames = dataFile.ColumnNames;
            
            foreach(var row in dataFile)
            {
                string s;
                float p, p2;

                var krow = new CsvRow(koinly);

                DateTime d = DateTime.Parse(row["Time"]);
                krow["Koinly Date"] = d.ToUniversalTime().ToString("yyyy-MM-dd HH:mm UTC");


                s = row["Symbol"];

                s = s.Replace("/", "-");

                krow["Pair"] = s.Replace(" PERP", "");

                krow["Side"] = row["Side"];

                s = row["Value"];
                s = s.Replace("USDT", "");
                p2 = float.Parse(s);

                s = row["Filled Price"];
                s = s.Replace("USDT", "");
                p = float.Parse(s);

                krow["Amount"] = row["Filled Amount(lot)"]; // (p2 / p).ToString();

                krow["Total"] = p2.ToString();

                s = row["Total commission"];

                s = s.Replace("USDT", "");
                if (!s.Contains("&lt;"))
                {
                    p = float.Parse(s);

                    if (p < 0f) p *= -1f;

                    krow["Fee Amount"] = p.ToString();
                }
                else
                {
                    krow["Fee Amount"] = "0.001";
                }

                krow["Fee Currency"] = "USDT";

                s = row["Transaction ID"];

                krow["Order ID"] = s;
                krow["Trade ID"] = s;

                koinly.AddRow(krow);
            }

            CsvToList(koinly, lvOut);

            /*

            Koinly Date,Pair,Side,Amount,Total,Fee Amount,Fee Currency,Order ID,Trade ID
            2018-01-01 14:25 UTC,BTC-USD,Buy,1,1000,5,USD,,

             
             */

        }

        private void txtFilename_TextChanged(object sender, EventArgs e)
        {
            EnableButtons();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (koinly != null)
            {

                var dlg = new SaveFileDialog()
                {
                    Title = "Export CSV File",
                    Filter = "Comma Separated Values Files (*.csv)|*.csv|Any File (*.*)|*.*",
                    OverwritePrompt = true
                };

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    koinly.SaveDocument(dlg.FileName);
                }

            }
        }

        private void btnCalc_Click(object sender, EventArgs e)
        {

            double total = 0.0f;

            double taxrate = float.Parse(txtTax.Text.Replace("$", ""));
            
            this.trades.Clear();

            foreach (var csvFile in openFiles)
            {
                if (File.Exists(csvFile))
                {
                    var dataFile = new CsvWrapper();
                    dataFile.OpenDocument(csvFile);

                    foreach (var row in dataFile)
                    {
                        var tentry = new TradeEntry(row);
                        if (tentry.Timestamp == default) continue;

                        if (!trades.Contains(tentry))
                        {
                            trades.Add(tentry);
                        }
                        else
                        {
                            try
                            {
                                Console.WriteLine("No.");
                            }
                            catch { }
                        }

                        var s = row["Closed Positions PNL"];

                        s = s.Replace("USDT", "");

                        float f;

                        if (float.TryParse(s, out f))
                        {
                            total += f;
                        }
                        else
                        {
                            var i = s.LastIndexOf("USD");
                            var n = 1;

                            if (string.IsNullOrEmpty(s)) continue;

                            if (s[0] == '-') n = -1;

                            if (i == -1) continue;

                            var j = s.LastIndexOf(" ", i);

                            if (j == -1) continue;

                            s = s.Substring(j + 1, i - j - 1).Trim();

                            s = s.Replace("USD", "");

                            if (float.TryParse(s, out f))
                            {
                                total += (f * n);

                            }
                        }

                    }
                }
            }
            
            total = 0;
            foreach (var trade in trades)
            {
                total += trade.PNL;
            }

            txtPnl.Text = "$" + total.ToString("#,##0.00");
            txtLiability.Text = "$" + (total * taxrate).ToString("#,##0.00");

        }

        private void txtTax_TextChanged(object sender, EventArgs e)
        {

        }

    }


}
