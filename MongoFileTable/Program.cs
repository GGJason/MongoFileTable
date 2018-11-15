using System;
using MongoDB.Driver;
using MongoDB.Bson;
using System.IO;
using System.Security.AccessControl;
namespace MongoFileTable
{
    class Program
    {
        static void Main(string[] args)
        {
            //var fileTable = MongoFileTable.Instance;
            //fileTable.Start();
            var fileTable = new FileTable("");
            fileTable.Start();
            Console.ReadLine();
        }
    }
}
