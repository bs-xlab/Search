using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Search.Modules.Models;

namespace Search
{
    public partial class MainForm : Form
    {
        readonly Worker Worker;
        static DateTime LastKeyPressTime;

        public MainForm()
        {
            InitializeComponent();
            Worker = Worker.GetInstance(formInterface: this);
            
            Task.Run(() => Worker.InitSystem());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void ChangeProgressBarValue(int newValue = 0, bool increment = false)
        {
            if (increment)
            {
                ExecuteExternalAction(() => Progress.Value++);
            }
            else
            {
                ExecuteExternalAction(() => Progress.Value = newValue);
            }
        }

        public void UnlockSearchBox()
        {
            ExecuteExternalAction(() =>
            {
                Search.Enabled = true;
                Progress.Visible = false;
            });
        }

        void ExecuteExternalAction(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }

        private void Search_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyValue > 64 && e.KeyValue < 91)
            {
                Task.Run(() =>
                {
                    LastKeyPressTime = DateTime.Now;
                    Thread.Sleep(500);

                    if ((DateTime.Now - LastKeyPressTime).TotalMilliseconds > 500)
                    {
                        ExecuteExternalAction(() =>
                        {
                            var k = Convert.ToInt32(label1.Text);
                            label1.Text = (++k).ToString();

                            ListBox.Items.Clear();
                            var result = Worker.FindProducts(Search.Text);

                            if (result.Count == 0)
                            {
                                ListBox.Height = ListBox.ItemHeight;
                                ListBox.Items.Add("No results...");
                            }
                            else
                            {
                                result.ForEach(r => ListBox.Items.Add(r));

                                ListBox.Height = result.Count < 10
                                    ? ListBox.ItemHeight * (result.Count + 1)
                                    : ListBox.ItemHeight * 10;
                            }
                        });
                    }
                });
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            label1.Visible = checkBox1.Checked;
            label2.Visible = checkBox1.Checked;
            label3.Visible = checkBox1.Checked;
            listBox1.Visible = checkBox1.Checked;
        }
    }
}