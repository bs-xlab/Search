using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Search
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            Task.Run(() => InitSystem());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        static public List<string> Syllables;
        Dictionary<Graph.Node, List<ProductModel>> ProductHashDict;

        public void InitSystem()
        {
            ExecuteExternalAction(() =>
            {
                ListBox.Height = 0;
                Search.Enabled = false;
            });
            
            // Data loading
            var data = LoadSystemData();

            var brands = CastFrom<BrandModel>(data);
            var groups = CastFrom<GroupModel>(data);
            var products = CastFrom<ProductModel>(data);

            #region Test model
            //groups = new List<GroupModel>
            //{
            //    new GroupModel { Name = "Плитка"},
            //    new GroupModel { Name = "Плитка для ванной"},
            //    new GroupModel { Name = "плитка для кухни"},
            //    new GroupModel { Name = "плитка для пола"},
            //    new GroupModel { Name = "фасадная плитка"},
            //};
            #endregion

            // ONE 5
            ExecuteExternalAction(() => Progress.Value = 5);

            // Deleting a redundant symbols
            groups.ForEach(g =>
            {
                g.Name = Regex.Replace(g.Name.ToLower(), "[^a-zа-я ]+", " ");
                while (g.Name.Contains("  ")) g.Name = g.Name.Replace("  ", " ");
            });

            brands.ForEach(b =>
            {
                b.Name = Regex.Replace(b.Name.ToLower(), "[^a-zа-я0-9 ]+", " ");
                while (b.Name.Contains("  ")) b.Name = b.Name.Replace("  ", " ");
            });

            // Craeting a syllables list
            Syllables = groups.SelectMany(g => g.Name.DeleteEndings().GetSeparatedString()).ToList();

            var brandSyllables = brands.SelectMany(b => 
                Regex.IsMatch(b.Name, "^[а-я0-9 ]+$") 
                    ? b.Name.GetSeparatedString() 
                    : b.Name.GetSeparatedStringEng())
                .ToList();

            Syllables.AddRange(brandSyllables);

            // Syllables sorting
            Syllables = Syllables
                .GroupBy(key => key)
                .ToDictionary(key => key.Key, value => value.Count())
                .OrderByDescending(k => k.Value)
                .Select(x => x.Key).ToList();

            // TWO 10
            ExecuteExternalAction(() => Progress.Value = 10);

            // Product filtering
            products = products.Where(p =>
                !(groups.FirstOrDefault(g => g.ID == p.GroupID) is null) &&
                !(brands.FirstOrDefault(b => b.ID == p.BrandID) is null))
                .ToList();

            // THREE 15
            ExecuteExternalAction(() => Progress.Value = 15);

            // Set products syllables
            products.ForEach(p =>
            {
                var brandName = brands.First(b => b.ID == p.BrandID).Name;
                var groupName = groups.First(g => g.ID == p.GroupID).Name;

                var _brandSyllables = Regex.IsMatch(brandName, "^[а-я0-9 ]+$")
                    ? brandName.GetSeparatedString()
                    : brandName.GetSeparatedStringEng();

                var brandIndexes = _brandSyllables.Select(s => Syllables.IndexOf(s)).Where(i => i != -1).ToList();

                var groupWords = groupName.Split(' ');
                var groupIndexesList = groupWords.Select(g => g.GetSeparatedString().Select(s => Syllables.IndexOf(s)).Where(i => i != -1).ToList()).ToList();

                p.IndexesList = new List<List<int>>();
                for (int offset = 0; offset < groupIndexesList.Count; offset++)
                {
                    var list = groupIndexesList.Skip(offset).Take(groupIndexesList.Count - offset).ToList();
                    list.AddRange(groupIndexesList.Take(offset));

                    p.IndexesList.Add(list.SelectMany(l => l).ToList());
                    p.IndexesList[p.IndexesList.Count - 1].AddRange(brandIndexes);

                    p.IndexesList.Add(brandIndexes);
                    p.IndexesList[p.IndexesList.Count - 1].AddRange(list.SelectMany(l => l).ToList());
                }

                p.Syllables = _brandSyllables;
                p.Syllables.AddRange(groups.First(g => g.ID == p.GroupID).Name.GetSeparatedString());

                p.FullIdentity = $"{groupName} {brandName} {p.Name}";
            });

            // FOUR 20
            ExecuteExternalAction(() => Progress.Value = 20);

            // Product Sorting
            products = products.OrderBy(p =>
                p.Syllables.Select(s => Syllables.IndexOf(s))
                    .Where(i => i != -1).Min())
                    .ThenByDescending(p => p.Syllables.Count)
                    .ToList();

            ExecuteExternalAction(() => Progress.Value = 15);
            ProductHashDict = new Dictionary<Graph.Node, List<ProductModel>>();

            // FIVE 25
            ExecuteExternalAction(() => Progress.Value = 25);
            var productCount = 1;

            // Hash dict creating
            products.ForEach(p =>
            {
                bool validHash = false;

                var hash = new bool[Syllables.Count];
                p.Syllables.ForEach(ps =>
                {
                    var index = Syllables.IndexOf(ps);
                    if (index != -1)
                    {
                        hash[index] = true;
                        validHash = true;
                    }
                });

                if (validHash)
                {
                    var node = new Graph.Node(hash);
                    if (!ProductHashDict.ContainsKey(node))
                    {
                        ProductHashDict[node] = new List<ProductModel>();
                    }
                    
                    ProductHashDict[node].Add(p);
                }

                if (products.Count > 60 && productCount % (products.Count / 60) == 0)
                {
                    ExecuteExternalAction(() => Progress.Value++);
                }
            });

            // SIX 85

            ExecuteExternalAction(() => Progress.Value = 85);
            var graph = Graph.GetInstance();

            // Graph building
            foreach (var productHashPair in ProductHashDict)
            {
                productHashPair.Value.ForEach(v => graph.AddNode(v.IndexesList));
            }


            // Graph buiulding
            //foreach (var productHashPair in ProductHashDict)
            //{
            //    productHashPair.Value.ForEach(p =>
            //    {
            //        var brandName = brands.First(b => b.ID == p.BrandID).Name;

            //        var syllables = Regex.IsMatch(brandName, "^[а-я0-9 ]+$")
            //            ? brandName.GetSeparatedString()
            //            : brandName.GetSeparatedStringEng();

            //        syllables.AddRange(groups.First(g => g.ID == p.GroupID).Name.GetSeparatedString());

            //        var indexesList = p.Name.Split(' ').Select(w => w.GetSeparatedString().Select(s => Syllables.IndexOf(s)).Where(i => i != -1).ToList()).ToList();
            //        graph.AddNode(indexesList);
            //    });
            //}

            ExecuteExternalAction(() =>
            {
                Search.Enabled = true;
                Progress.Value = 99;

                Thread.Sleep(2000);
                Progress.Visible = false;
            });
        }

        List<BriefModel> LoadSystemData()
        {
            var brands = new List<BrandModel>();
            var groups = new List<GroupModel>();
            var products = new List<ProductModel>();

            if (File.Exists("BrandModel.json") && File.Exists("GroupModel.json") && File.Exists("ProductModel.json"))
            {
                brands = LoadList<BrandModel>();
                groups = LoadList<GroupModel>();
                products = LoadList<ProductModel>();
            }
            else
            {
                throw new NotImplementedException();

                //using (var db = new MySQLconnections(MysqlConnectionStrings.Central))
                //{
                //    brands = db.Query<BrandModel>("SELECT ID, Name FROM `z-price`.brands;");
                //    groups = db.Query<GroupModel>("SELECT ID, Name FROM `z-price`.groups;");
                //    products = db.Query<ProductModel>("SELECT ProductID AS 'ID', BrandID, GroupID, ModelName AS 'Name' FROM `z-price`.products;");

                //    SaveList(brands);
                //    SaveList(groups);
                //    SaveList(products);
                //}
            }

            var result = new List<BriefModel>();

            result.AddRange(brands);
            result.AddRange(groups);
            result.AddRange(products);

            return result;
        }

        List<T> LoadList<T>() where T : class
        {
            var serialiaedData = File.ReadAllText($"{typeof(T).Name}.json");
            return JsonConvert.DeserializeObject<List<T>>(serialiaedData);
        }

        void SaveList<T>(List<T> targetList) where T : class
        {
            var serializedData = JsonConvert.SerializeObject(targetList);
            File.WriteAllText($"{typeof(T).Name}.json", serializedData);
        }

        List<T> CastFrom<T>(List<BriefModel> data)
        {
            return data.Where(d => d is T).Cast<T>().ToList();
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

        static DateTime LastKeyPressTime;

        private void Search_KeyUp(object sender, KeyEventArgs e)
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

                        var input = Search.Text;

                        var sw = new Stopwatch();
                        sw.Start();

                        var outputs = Graph.GetInstance().FindString(input);
                        ListBox.Items.Clear();
                        //listBox1.Items.Clear();

                        if (outputs is null)
                        {
                            ListBox.Height = ListBox.ItemHeight;
                            ListBox.Items.Add("No results...");
                        }
                        else
                        {
                            var count = 0;
                            outputs.ForEach(c =>
                            {
                                if (ProductHashDict.ContainsKey(c))
                                {
                                    ProductHashDict[c].ForEach(p =>
                                    {
                                        count++;
                                        ListBox.Items.Add(p.FullIdentity);
                                    });
                                }
                            });


                            ListBox.Height = count < 10 ? ListBox.ItemHeight * (count + 1): ListBox.ItemHeight * 10;
                        }

                        sw.Stop();
                        label2.Text = sw.Elapsed.TotalSeconds.ToString();

                        //sw.Start();
                        //var groupNames = groups.Select(g => g.Name).ToList();
                        
                        //groupNames.ForEach(g =>
                        //{
                        //    if (g.Contains(input))
                        //    {
                        //        listBox1.Items.Add(g);
                        //    }
                        //});
                        
                        //sw.Stop();
                        //label3.Text = sw.Elapsed.TotalSeconds.ToString();
                    });
                }
            });
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