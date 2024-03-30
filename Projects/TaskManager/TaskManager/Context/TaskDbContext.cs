using MongoDB.Driver;
using TaskManager.Utils;

namespace TaskManager.Context
{
    public class TaskDbContext
    {
        public readonly IMongoDatabase Database;

        public TaskDbContext(MongoDbConfig config)
        {
            var client = new MongoClient(config.ConnectionString);
            Database = client.GetDatabase(config.DatabaseName);
        }
    }
}
