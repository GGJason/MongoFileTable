using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;
using System.IO;
namespace MongoFileTable
{
    class FileTable
    {
        private static readonly object Mutex = new object();
         string storagePath = "FileStorage";
         string mongoDBName = "FILE";
        IMongoDatabase mongoDB;// = mongoClient.GetDatabase(mongoDBName);
        IMongoCollection<MongoFile> fileCollection;// = mongoDB.GetCollection<MongoFile>("file");
         MongoClient mongoClient = new MongoClient("mongodb://webimviewer:Qwer1234@webimviewertestcluster-shard-00-00-vfvrr.mongodb.net:27017,webimviewertestcluster-shard-00-01-vfvrr.mongodb.net:27017,webimviewertestcluster-shard-00-02-vfvrr.mongodb.net:27017/test?ssl=true&replicaSet=WeBIMViewerTestCluster-shard-0&authSource=admin&retryWrites=true");

        static private FileTable _currentTable = new FileTable();
        public FileTable() { throw new NotImplementedException(); }
        public FileTable(string host, string username = "", string password = "", IList<int> port=null, string database = "", IList<string> options=null)
        {
            mongoDB = mongoClient.GetDatabase(mongoDBName);
            fileCollection = mongoDB.GetCollection<MongoFile>("file");

        }
        static public FileTable Instance
        {
            get
            {
                if (_currentTable == null)
                {
                    lock (Mutex)
                    {
                        _currentTable = new FileTable();
                    }
                }
                return _currentTable;
            }
        }

        public void Start() { 
        if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath); 

            var watcher = new FileSystemWatcher()
            {
                Path = storagePath,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
        watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Created;
            watcher.Deleted += Watcher_Deleted;
            watcher.Renamed += Watcher_Renamed;
            Console.Read();
        }

    private  void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            FileInfo info = new FileInfo(e.FullPath);
            fileCollection.FindOneAndUpdate(
                     new FilterDefinitionBuilder<MongoFile>().Eq(f => f.Path, e.OldFullPath),
                     new UpdateDefinitionBuilder<MongoFile>().Set(f => f.SystemName, e.Name)
                     .Set(f=>f.Extension,info.Extension));

            Console.WriteLine($"RENAME {e.OldName} TO {e.Name} ");
    }

    private  void Watcher_Deleted(object sender, FileSystemEventArgs e)
    {
            fileCollection.DeleteOne(
                     new FilterDefinitionBuilder<MongoFile>().Eq(f => f.Path, e.FullPath));
        Console.WriteLine($"DELETE {e.Name}");
    }

    private  void Watcher_Created(object sender, FileSystemEventArgs e)
    {
            if (File.GetAttributes(e.FullPath) == FileAttributes.Directory)
            {

            }
            else
            {

                FileInfo info = new FileInfo(e.FullPath);
                var file = new MongoFile()
                {
                    _id = new ObjectId(),
                    SystemName = e.Name,
                    CreatedAt = File.GetCreationTimeUtc(e.FullPath),
                    EditedAt = File.GetLastWriteTimeUtc(e.FullPath),
                    Path = e.FullPath,
                    Attributes = info.Attributes.ToString(),
                    Length = info.Length,
                    Extension = info.Extension
                };
                fileCollection.InsertOne(file);
            }
            Console.WriteLine($"CREATE {e.Name}");
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                if (File.GetAttributes(e.FullPath) == FileAttributes.Directory)
                {

                }
                else
                {
                    FileInfo info = new FileInfo(e.FullPath);
                    fileCollection.FindOneAndUpdate(
                        new FilterDefinitionBuilder<MongoFile>().Eq(f => f.Path, e.FullPath),
                        new UpdateDefinitionBuilder<MongoFile>().Set(f => f.EditedAt, File.GetLastWriteTimeUtc(e.FullPath))
                        .Set(f => f.Length, info.Length));
                }
            }
            else
            {
                fileCollection.DeleteOne(new FilterDefinitionBuilder<MongoFile>().Eq(f => f.Path, e.FullPath));
            }
          
        Console.WriteLine($"CHANGE {e.Name}");
    }
}
}
