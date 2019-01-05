using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pe.itsight.Util;
using MongoDB.Driver;
using MongoDB.Bson;
using pe.itsight.Entities;
using Tweetinvi;
using pe.itsight.apismartia;

namespace pe.itsight.apiSmartIA
{
    class Program
    {
        static void Main(string[] args)
        {
            new TaskCore().ProcesarAnalisis();
        }






        public async void readmongo()
        {
            await MongoAsync();
            var db = MongoAsync().Result;

            var collection = db.GetCollection<Analysis>("analysis");

            var list = collection.AsQueryable<Analysis>().ToList();

            //var newUsers = CreateNewUsers();
            //collection.InsertManyAsync(newUsers);

            foreach (var dox in list)
            {
                Console.WriteLine(dox.nameKey);
            }

        }



        static async Task<IMongoDatabase> MongoAsync()
        {
            string urlMongo = "mongodb://localhost";
            var cliente = new MongoClient(urlMongo);
            return cliente.GetDatabase("ARMongo");
        }
        
        //private static IEnumerable<User> CreateNewUsers()
        //{
        //    var user1 = new User
        //    {
        //        name = "Jean",
        //        age = 20
        //    };
        //    var user2 = new User
        //    {
        //        name = "Marts",
        //        age = 21
        //    };
        //    var user3 = new User
        //    {
        //        name = "Julios",
        //        age = 22
        //    };
        //    var user4 = new User
        //    {
        //        name = "Peters",
        //        age = 23
        //    };

        //    var newStudents = new List<User> { user1, user2, user3 , user4 };

        //    return newStudents;
        //}


    }
}
