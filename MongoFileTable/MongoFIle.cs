using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoFileTable
{
    public class MongoFile
    {
        public ObjectId _id = new ObjectId();
        public Guid PublicId = new Guid();
        public string Name;
        public string SystemName;
        public DateTime CreatedAt;
        public DateTime EditedAt;
        public string Path;
        public string Extension;
        public string Attributes;
        public long Length;
    }
}