using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Security.Authentication;

namespace fonctionmongo
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {

            string connectionString =
  @"mongodb://xxxxxxxxxxmongo.cosmos.azure.com:10255/?ssl=true&replicaSet=globaldb&maxIdleTimeMS=120000&appName=@edetestnew@&retrywrites=false";
            MongoClientSettings settings = MongoClientSettings.FromUrl(
              new MongoUrl(connectionString)
            );
            settings.SslSettings =
              new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            var mongoClient = new MongoClient(settings);




            //Database List  
           

     
            //Get Database and Collection  
            IMongoDatabase db = mongoClient.GetDatabase("YOURDB");
             var personColl = db.GetCollection<BsonDocument>("YOURBACKUP");
             var coll = db.GetCollection<BsonDocument>("YOURCOLLECTION");


            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
    .Match(change => change.OperationType == ChangeStreamOperationType.Insert || change.OperationType == ChangeStreamOperationType.Update || change.OperationType == ChangeStreamOperationType.Replace)
    .AppendStage<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>, BsonDocument>("{ $project: { 'fullDocument': 1, 'ns': 1, 'documentKey': 1 }}");
         
                
                
           //     "{ $project: { '_id': 1, 'fullDocument': 1, 'ns': 1, 'documentKey': 1 }}");


            var options = new ChangeStreamOptions
            {
             //   FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup

            };

            var enumerator = coll.Watch(pipeline, options).ToEnumerable().GetEnumerator();


       

            while (enumerator.MoveNext())
            {

                BsonDocument doc = new BsonDocument();

                


                doc = enumerator.Current.ToBsonDocument();

                // add date to look for restore easily 
                doc.Add(new BsonElement("date", DateTime.Now));

                if (doc.Contains("_id"))
                    doc.Remove("_id");
            


                log.LogInformation($"C# Timer trigger function executed at: {doc.ToString()}");


                 personColl.InsertOne(doc);
            }

            enumerator.Dispose();



                 





            
        }
    }
}
