using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Search.Modules.Helpers;

namespace Search.Modules.Models
{
    class Worker
    {
        static Worker Instance;
        readonly MainForm FormInterface;
        Graph Graph;

        static public List<string> Syllables;
        Dictionary<Graph.Node, List<ProductModel>> ProductHashDict;

        public static Worker GetInstance(MainForm formInterface)
        {
            if (Instance is null)
            {
                Instance = new Worker(formInterface);
            }

            return Instance;
        }

        Worker(MainForm formInterface)
        {
            FormInterface = formInterface;
        }

        public void InitSystem()
        {
            // Data loading
            var data = Repository.LoadSystemData();

            var brands = Repository.CastFrom<BrandModel>(data);
            var groups = Repository.CastFrom<GroupModel>(data);
            var products = Repository.CastFrom<ProductModel>(data).Take(50000).ToList();

            #region Test product model
            //var products = new List<ProductModel>
            //{
            //    new ProductModel { BrandID = 47, GroupID = 21, Name = "A-0822 R", ID = 8569}
            //};
            #endregion

            #region Test group model
            //groups = new List<GroupModel>
            //{
            //    new GroupModel { Name = "Плитка"},
            //    new GroupModel { Name = "Плитка для ванной"},
            //    new GroupModel { Name = "плитка для кухни"},
            //    new GroupModel { Name = "плитка для пола"},
            //    new GroupModel { Name = "фасадная плитка"},
            //};
            #endregion

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
            Syllables = groups.SelectMany(g => g.Name.GetSyllablesWithoutEndings()).ToList();

            var brandSyllables = brands.SelectMany(b =>
                Regex.IsMatch(b.Name, "^[а-я0-9 ]+$")
                    ? b.Name.GetSyllablesWithoutEndings()
                    : b.Name.GetSeparatedStringEng())
                .ToList();

            Syllables.AddRange(brandSyllables);

            // Syllables sorting
            Syllables = Syllables
                .GroupBy(key => key)
                .ToDictionary(key => key.Key, value => value.Count())
                .OrderByDescending(k => k.Value)
                .Select(x => x.Key).ToList();

            // Product filtering - 75 sec
            products = products.Where(p =>
               !(groups.FirstOrDefault(g => g.ID == p.GroupID) is null) &&
               !(brands.FirstOrDefault(b => b.ID == p.BrandID) is null) &&
               p.BrandID != 0 && p.GroupID != 0)
               .ToList();
            
            FormInterface.ChangeProgressBarValue(4);

            // Set products syllables
            products.ForEach(p =>
            {
                var brandName = brands.First(b => b.ID == p.BrandID).Name;
                var groupName = groups.First(g => g.ID == p.GroupID).Name;

                var _brandSyllables = Regex.IsMatch(brandName, "^[а-я0-9 ]+$")
                    ? brandName.GetSyllablesWithoutEndings()
                    : brandName.GetSeparatedStringEng();

                var brandIndexes = _brandSyllables.Select(s => Syllables.IndexOf(s)).Where(i => i != -1).ToList();
                var groupIndexesList = groupName.Split(' ').Select(g => g.GetSyllablesWithoutEndings().Select(s => Syllables.IndexOf(s)).Where(i => i != -1).ToList()).ToList();

                p.IndexesList = new List<List<int>>();
                for (int offset = 0; offset < groupIndexesList.Count; offset++)
                {
                    var list = groupIndexesList.Skip(offset).Take(groupIndexesList.Count - offset).ToList();
                    list.AddRange(groupIndexesList.Take(offset));

                    p.IndexesList.Add(list.SelectMany(l => l).ToList());
                    p.IndexesList[p.IndexesList.Count - 1].AddRange(brandIndexes);

                    p.IndexesList.Add(brandIndexes.Select(b => b).ToList());
                    p.IndexesList[p.IndexesList.Count - 1].AddRange(list.SelectMany(l => l).ToList());
                }

                p.Syllables = _brandSyllables;
                p.Syllables.AddRange(groups.First(g => g.ID == p.GroupID).Name.GetSyllablesWithoutEndings());

                p.FullIdentity = $"{groupName} {brandName} {p.Name}";
            });
            
            FormInterface.ChangeProgressBarValue(14);

            // Product Sorting
            products = products.OrderBy(p =>
                p.Syllables.Select(s => Syllables.IndexOf(s))
                    .Where(i => i != -1).Min())
                    .ThenByDescending(p => p.Syllables.Count)
                    .ToList();

            ProductHashDict = new Dictionary<Graph.Node, List<ProductModel>>();
            
            FormInterface.ChangeProgressBarValue(15);
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

                if (products.Count > 84 && productCount++ % (products.Count / 84) == 0)
                {
                    FormInterface.ChangeProgressBarValue(increment: true);
                }
            });
            
            FormInterface.ChangeProgressBarValue(99);
            Graph = Graph.GetInstance(Syllables.Count);

            // Graph building
            foreach (var productHashPair in ProductHashDict)
            {
                productHashPair.Value.ForEach(v => Graph.AddNode(v.IndexesList));
            }

            FormInterface.UnlockSearchBox();
        }

        public List<string> FindProducts(string input)
        {
            var result = new List<string>();
            var output = Graph.FindString(input, Syllables);

            output.Result.Sort(new NodesSorter(output.FoundedNode));
            var count = 0;

            output.Result.ForEach(c =>
            {
                if (ProductHashDict.ContainsKey(c))
                {
                    ProductHashDict[c].ForEach(p =>
                    {
                        count++;
                        result.Add(p.FullIdentity);
                    });
                }
            });

            return result;
        }
    }
}