using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pe.itsight.Entities
{
    public class User
    {
        public ObjectId id { get; set; }
        public string name { get; set; }
        public int age { get; set; }
        public string _class { get; set; }
    }
}
