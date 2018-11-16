using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;
using System.IO;
using System.Linq;

namespace MongoFileTable
{
    class MongoFileTable
    {
        private static readonly object Mutex = new object();
        string storagePath = "FileStorage";
        string mongoDBName = "FILE";
        IMongoDatabase mongoDB;// = mongoClient.GetDatabase(mongoDBName);
        IMongoCollection<MongoFile> fileCollection;// = mongoDB.GetCollection<MongoFile>("file");
        MongoClient mongoClient = new MongoClient("mongodb://webimviewer:Qwer1234@webimviewertestcluster-shard-00-00-vfvrr.mongodb.net:27017,webimviewertestcluster-shard-00-01-vfvrr.mongodb.net:27017,webimviewertestcluster-shard-00-02-vfvrr.mongodb.net:27017/test?ssl=true&replicaSet=WeBIMViewerTestCluster-shard-0&authSource=admin&retryWrites=true");

        static private MongoFileTable _currentTable = new MongoFileTable();
        public MongoFileTable() { throw new NotImplementedException(); }
        public MongoFileTable(string host, string username = "", string password = "", IList<int> port = null, string database = "", IList<string> options = null)
        {
            mongoDB = mongoClient.GetDatabase(mongoDBName);
            fileCollection = mongoDB.GetCollection<MongoFile>("file");

        }
        static public MongoFileTable Instance
        {
            get
            {
                if (_currentTable == null)
                {
                    lock (Mutex)
                    {
                        _currentTable = new MongoFileTable();
                    }
                }
                return _currentTable;
            }
        }
        public DownloadFileStreamData GetStreamByPublicId(string PublicId)
        {
            var files = fileCollection.Find<MongoFile>(new FilterDefinitionBuilder<MongoFile>().Eq(f => f.PublicId, new Guid(PublicId)));
            var first = files.First();
            if (File.Exists(first.Path))
            {
                return new DownloadFileStreamData(first.Path.Split('/').Last(), new FileStream(first.Path, FileMode.Open));
            }
            else
                throw new FileNotFoundException();
        }
        public DownloadFileStreamData GetStreamByObjectId(string PublicId)
        {
            var files = fileCollection.Find<MongoFile>(new FilterDefinitionBuilder<MongoFile>().Eq(f => f._id, new ObjectId(PublicId)));
            var first = files.First();
            if (File.Exists(first.Path))
            {
                return new DownloadFileStreamData(first.Name, new FileStream(first.Path, FileMode.Open));
            }
            else
                throw new FileNotFoundException();
        }
        public void Update(string id, Stream stream, string name = "")
        {
            var result = fileCollection.Find(new FilterDefinitionBuilder<MongoFile>().Eq(f => f._id, new ObjectId(id)));

            var path = result.First().Path;
            var update = result.First();
            if (name != "")
            {
                update.Name = name;
                update.Extension = Path.GetExtension(name);
                update.Path = Path.ChangeExtension(update.Path, update.Extension);
                var filter = new FilterDefinitionBuilder<MongoFile>().Eq(f => f._id, update._id);
                var up = new ObjectUpdateDefinition<MongoFile>(update);
                var up2 = new UpdateDefinitionBuilder<MongoFile>().Set(f => f.Name, update.Name)
                    .Set(f => f.Extension, update.Extension)
                    .Set(f => f.Path, update.Path);
                fileCollection.UpdateOne(filter, up2);
            }
            var fs = new FileStream(path, FileMode.Create);
            byte[] bytes = new byte[1024];
            int num = 0;
            do
            {
                num = stream.Read(bytes, 0, 1024);
                Console.WriteLine(bytes);
                fs.Write(bytes, 0, num);
            } while (num > 0);
            fs.Close();
            stream.Close();
            File.Move(path, update.Path);
        }
        public ObjectId Upload(string Name, Stream stream)
        {
            var objectId = ObjectId.GenerateNewId(DateTime.UtcNow);
            var systemName = $"{objectId}_{Name}";
            var fullPath = storagePath + "/" + systemName;
            if (fileCollection.Find<MongoFile>(new FilterDefinitionBuilder<MongoFile>().Eq(f => f.Path, fullPath)).CountDocuments() == 0)
            {
                var file = new MongoFile()
                {
                    _id = objectId,
                    PublicId = Guid.NewGuid(),
                    SystemName = Name,
                    CreatedAt = DateTime.UtcNow,
                    EditedAt = DateTime.UtcNow,
                    Path = fullPath,
                    Length = stream.Length,
                    Extension = Path.GetExtension(Name),
                };
                //fileCollection.BulkWrite(model);
                fileCollection.InsertOne(file);
                //  fileCollection.(new FilterDefinitionBuilder<MongoFile>().Eq(f=>f._id,id),file);
            }
            FileStream fs = new FileStream(storagePath + "/" + systemName, FileMode.Create);
            byte[] bytes = new byte[1024];
            int num = 0;
            do
            {
                num = stream.Read(bytes, 0, 1024);
                Console.WriteLine(bytes);
                fs.Write(bytes, 0, num);
            } while (num > 0);
            stream.Close();
            fs.Close();
            fs.Dispose();
            fileCollection.UpdateOne(new FilterDefinitionBuilder<MongoFile>().Eq(f => f._id, objectId),
                new UpdateDefinitionBuilder<MongoFile>().Set(f => f.Name, Name));


            stream.Close();
            fs.Close();

            return objectId;
        }
        public void Start()
        {
            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);

            var watcher = new FileSystemWatcher()
            {
                Path = storagePath,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
            };
            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Created;
            watcher.Deleted += Watcher_Deleted;
            watcher.Renamed += Watcher_Renamed;

        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            FileInfo info = new FileInfo(e.FullPath);
            fileCollection.FindOneAndUpdate(
                     new FilterDefinitionBuilder<MongoFile>().Eq(f => f.Path, e.OldFullPath),
                     new UpdateDefinitionBuilder<MongoFile>().Set(f => f.SystemName, e.Name)
                     .Set(f => f.Extension, info.Extension));

            Console.WriteLine($"RENAME {e.OldName} TO {e.Name} ");
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            fileCollection.DeleteOne(
                     new FilterDefinitionBuilder<MongoFile>().Eq(f => f.Path, e.FullPath));
            Console.WriteLine($"DELETE {e.Name}");
        }

        private async void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (File.GetAttributes(e.FullPath) == FileAttributes.Directory)
            {

            }
            else
            {

                FileInfo info = new FileInfo(e.FullPath);
                var array = e.Name.Split('_');
                if (fileCollection.Find<MongoFile>(new FilterDefinitionBuilder<MongoFile>().Eq(f => f._id, new ObjectId(array[0]))).CountDocuments() == 0)
                {
                    var id = new ObjectId(array[0]);

                    var file = new MongoFile()
                    {
                        _id = id,
                        PublicId = Guid.NewGuid(),
                        SystemName = e.Name,
                        CreatedAt = File.GetCreationTimeUtc(e.FullPath),
                        EditedAt = File.GetLastWriteTimeUtc(e.FullPath),
                        Path = e.FullPath,
                        Attributes = info.Attributes.ToString(),
                        Length = info.Length,
                        Extension = info.Extension
                    };
                    var update = new ObjectUpdateDefinition<MongoFile>(file);

                    var model = new List<WriteModel<MongoFile>>(){new UpdateOneModel<MongoFile>(
                        new FilterDefinitionBuilder<MongoFile>().Eq(f => f._id, id),
                        update
                        )
                    { IsUpsert = true} };
                    //fileCollection.BulkWrite(model);
                    await fileCollection.InsertOneAsync(file);
                    //  fileCollection.(new FilterDefinitionBuilder<MongoFile>().Eq(f=>f._id,id),file);
                }

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
