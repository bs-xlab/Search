using Newtonsoft.Json;
using Search.Modules.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Search.Modules.Helpers
{
    public static class Repository
    {
        public static List<BriefModel> LoadSystemData()
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

        public static List<T> LoadList<T>() where T : class
        {
            var serialiaedData = File.ReadAllText($"{typeof(T).Name}.json");
            return JsonConvert.DeserializeObject<List<T>>(serialiaedData);
        }

        public static void SaveList<T>(List<T> targetList) where T : class
        {
            var serializedData = JsonConvert.SerializeObject(targetList);
            File.WriteAllText($"{typeof(T).Name}.json", serializedData);
        }

        public static List<T> CastFrom<T>(List<BriefModel> data)
        {
            return data.Where(d => d is T).Cast<T>().ToList();
        }
    }
}