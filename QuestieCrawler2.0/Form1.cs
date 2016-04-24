using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuestieCrawler2._0
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public void ToggleButtons(bool Enabled)
        {
            this.Invoke((MethodInvoker)delegate()
            {
                GetAllQuestsButtons.Enabled = Enabled;
            });
        }

        public void PrintDebugMessage(string Msg)
        {
            this.Invoke((MethodInvoker)delegate()
            {
                debuglistBox.Items.Add(Msg);
                int visibleItems = debuglistBox.ClientSize.Height / debuglistBox.ItemHeight;
                debuglistBox.TopIndex = Math.Max(debuglistBox.Items.Count - visibleItems + 1, 0);
            });
        }

        Dictionary<int, List<Quest>> Quests = new Dictionary<int, List<Quest>>();//int = Zone int[] = Quests

        private void GetAllQuests()
        {
            StatusLabel.Invoke((MethodInvoker)delegate()
            {
                StatusLabel.Text = "Fetching all quests";
            });
            PrintDebugMessage("Function: Fetching All quests");
            progressBar1.Invoke((MethodInvoker)delegate()
            {
                progressBar1.Step = 1;
                progressBar1.Value = 0;
                progressBar1.Maximum = 3;
            });

            ToggleButtons(false);
            string URL = "https://db.rising-gods.de/?zones=";

            Web w = new Web();
            List<int> ContinentIDS = new List<int>();
            ContinentIDS.Add(0);
            ContinentIDS.Add(1);
            ContinentIDS.Add(8);

            Dictionary<int, string[]> Zones = new Dictionary<int, string[]>();
            foreach (int z in ContinentIDS)
            {
                w.Navigate(URL + z);
                Object Read = (Object)w.GetVar("g_listviews['zones']['data']['length']");
                if (Read == null) { return; }

                int ZoneLength = int.Parse(Read.ToString());
                //ReadOnlyCollection<Object> read = (ReadOnlyCollection<Object>)r;
                //int ZoneLength = int.Parse(web.GetVar("g_listviews['zones']['data']['length']").ToString());
                string DataPath = "g_listviews['zones']['data']";

                for (int i = 0; i < ZoneLength; i++)
                {
                    string Name = w.GetVar(DataPath + "['" + i + "']['name']").ToString();
                    int ID = int.Parse(w.GetVar(DataPath + "['" + i + "']['id']").ToString());
                    Zones.Add(ID, new string[] { Name, z.ToString() });
                }
                progressBar1.Invoke((MethodInvoker)delegate()
                {
                    progressBar1.PerformStep();
                });
            }
            progressBar1.Invoke((MethodInvoker)delegate()
            {
                progressBar1.Maximum = progressBar1.Maximum + Zones.Count;
            });
            int QuestsAdded = 0;
            foreach(KeyValuePair<int, string[]> Z in Zones)
            {
                List<Quest> Quests = new List<Quest>();
                int Zone = Z.Key;
                int Continent = int.Parse(Z.Value[1]);
                string ZoneName = Z.Value[0];
                URL = "https://www.burning-crusade.com/database/?quests=" + Continent + "." + Zone;
                w.Navigate(URL);
                int length = -1;
                try
                {
                    length = int.Parse(w.GetVar("g_listviews['quests']['data']['length']").ToString());
                }
                catch
                {
                    PrintDebugMessage("!Error! fetching Zone:" + Zone + " with name: " + ZoneName);
                    continue;
                }
                for (int i = 0; i < length; i++)
                {
                    int ID = int.Parse(w.GetVar("g_listviews['quests']['data']['" + i + "']['id']").ToString());
                    string Name = w.GetVar("g_listviews['quests']['data']['" + i + "']['name']").ToString();
                    int Level = int.Parse(w.GetVar("g_listviews['quests']['data']['" + i + "']['level']").ToString());
                    int ReqLevel = Level;
                    try
                    {
                        ReqLevel = int.Parse(w.GetVar("g_listviews['quests']['data']['" + i + "']['reqlevel']").ToString());
                    }
                    catch
                    {
                        PrintDebugMessage("!Error! Got no ReqLevel for the quest:"+ID+":"+Name+" used questlevel instead!");
                    }
                    int Side = int.Parse(w.GetVar("g_listviews['quests']['data']['"+i+"']['side']").ToString());
                    Quests.Add(new Quest());
                    Quests[Quests.Count - 1].ID = ID;
                    Quests[Quests.Count - 1].Name = Name;
                    Quests[Quests.Count - 1].Level = Level;
                    Quests[Quests.Count - 1].ReqLevel = ReqLevel;
                    Quests[Quests.Count - 1].Side = Side;
                    QuestsAdded++;
                    StatusLabel.Invoke((MethodInvoker)delegate()
                    {
                        StatusLabel.Text = "Quests Added: " + QuestsAdded;
                    });
                }
                progressBar1.Invoke((MethodInvoker)delegate()
                {
                    progressBar1.PerformStep();
                });
                this.Quests.Add(Zone, Quests);
            }





            PrintDebugMessage("DONE: " + "Fetched " + QuestsAdded + " quests!");
            w.Quit();
            progressBar1.Invoke((MethodInvoker)delegate()
            {
                progressBar1.Value = progressBar1.Maximum;
            });
            ToggleButtons(true);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(GetAllQuests);
            t.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = progressBar1.Value+"/"+progressBar1.Maximum;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach(Web w in Web.ActiveWeb)
            {
                w.driver.Close();
            }
        }
    }
}
