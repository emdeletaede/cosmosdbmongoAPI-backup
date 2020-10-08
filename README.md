---
page_type: sample
languages:
- c# , mongo shell 
products:
- azure-cosmosdb 
description: "This sample demonstrates a sample of how changestream can help you to build your own backup system for a collection and help you to restore in case of corruption in a cosmosdb database "

---
# build a backup system for comsosdb mongo API without entreprise tools 

## About this sample

> This sample demostrate how to build in addition of the existing backup policies that you can find here  
https://docs.microsoft.com/en-us/azure/cosmos-db/online-backup-and-restore 
a more granular backup for the cosmosdb mongo API. 


### Overview

This sample demonstrates an azure fonction call by timer that will take all the change in cosmosdb mongo API using changestream and copie in a collection the document with more information to retrieve and have the capacity to copy back 


1. you will have the code of the function that will connect to one collection and copie all the change in one collection , you can adapt and have multiple collection that will have one backup collection , 



## How to run this sample

To run this sample, you'll need:

> - a visual studio to load the function and run and deploy the function . 
> - An Azure cosmosdb account with mongo API 


### Step 1:  Clone or download this repository

From your shell or command line:

```Shell
git clone https://github.com/emdeletaede/cosmosdbmongoapi-backup.git
```

or download and extract the repository .zip file.

> Given that the name of the sample is quite long, you might want to clone it in a folder close to the root of your hard drive, to avoid file name length limitations when running on Windows.

### Step 2:  prepare you environement 


- You will need to setup the cosmosdb mongo API , in my case i will use the same endpoint , but you can write to another endpoint too.... just adapt the code 

so create an azure fonction using timer oclock like describe here 
https://docs.microsoft.com/en-gb/azure/azure-functions/functions-create-scheduled-function or 
https://techinfocorner.com/post/create-timer-trigger-azure-function-using-visual-studio-2019#:~:text=Open%20Microsoft%20Visual%20Studio%202019,run%20for%20every%20five%20seconds.

let s see the code generate 



```c# 

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
 
namespace AzureTimerTriggerFuncExp
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("*/5 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}


```


you can change the ([TimerTrigger("*/5 * * * * *")] and if you want to execute every 5 mn you can change by 
[TimerTrigger("0 */5 * * * *")] just delete one star ,of course you can adapt to 1 mn or less.... depend on your need ... 



after i will change the code by the following code and load the mongodb drivers for C#


```c# 

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Security.Authentication; // this is for sslprotocals TLS 

namespace fonctionmongo
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {

// define your connection string to your endpoint don't forget &retrywrites=false"; due to change in mongo 3.6 

            string connectionString =
  @"mongodb://account:key@account.mongo.cosmos.azure.com:10255/?ssl=true&replicaSet=globaldb&maxIdleTimeMS=120000&appName=@account@&retrywrites=false";
            MongoClientSettings settings = MongoClientSettings.FromUrl(
              new MongoUrl(connectionString)
            );
            settings.SslSettings =
              new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            var mongoClient = new MongoClient(settings);

     
            //Get Database and Collection  
            IMongoDatabase db = mongoClient.GetDatabase("YOurDB ");
             var backupColl = db.GetCollection<BsonDocument>("booksbackup");
             var coll = db.GetCollection<BsonDocument>("books");

// enable the changestream here 

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
    .Match(change => change.OperationType == ChangeStreamOperationType.Insert || change.OperationType == ChangeStreamOperationType.Update || change.OperationType == ChangeStreamOperationType.Replace)
    .AppendStage<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>, BsonDocument>("{ $project: { 'fullDocument': 1, 'ns': 1, 'documentKey': 1 }}");
         

            var options = new ChangeStreamOptions
            {
            
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup

            };

            var enumerator = coll.Watch(pipeline, options).ToEnumerable().GetEnumerator();

// get the changestream information 
       

            while (enumerator.MoveNext())
            {
  // create a new document and copy the enumerator 
                BsonDocument doc = new BsonDocument();
                doc = enumerator.Current.ToBsonDocument();

                // add date to look for restore easily or another field how you want 
                doc.Add(new BsonElement("date", DateTime.Now));
// delete some metafata generate by the changestream 
                if (doc.Contains("_id"))
                    doc.Remove("_id");
            
// log the information in case of debug can be // 

            //     log.LogInformation($"C# Timer trigger function executed at: {doc.ToString()}");


                 backupCColl.InsertOne(doc);
            }

            enumerator.Dispose();



                 
            
        }
    }
}


```
before to launch this application create the target collection booksbackup and define the partition key , in my case i have decides to put like partition key the "documentKey/_id" , like this a restore document by document will be more easily to find ... but you can change and take what you want 

and run this information in my case i have a book collecton with the following document inside 
```json
{
	"_id" : 1,
	"item" : "test",
	"stock" : 32
}
---
you can use the shell to update the mongo information and see what happen , when you update , create a document you will have the follwing document 


```json
{
	"_id" : {
		"_data" : {
			"$binary" : "IjE0NzYi",
			"$type" : "00"
		},
		"_kind" : 1
	},
	"fullDocument" : {
		"_id" : 1,
	"item" : "test",
	"stock" : 32
	},
	"ns" : {
		"db" : "edetest",
		"coll" : "books"
	},
	"documentKey" : {
		"_id" : 1
	},
	"date" : {
		"$date" : 1602140753972
	}
}
---

, in my code sample when i made 


```c#
       if (doc.Contains("_id"))
                    doc.Remove("_id");*
---

so what happen i have the following document 

```json
{
	{
	"_id" : ObjectId("5f7ec8f9bd5b74a879d0b1d1"),
	"fullDocument" : {
		"_id" : 1,
	"item" : "test",
	"stock" : 32
	},
	"ns" : {
		"db" : "edetest",
		"coll" : "books"
	},
	"documentKey" : {
		"_id" : 1
	},
	"date" : {
		"$date" : 1602140753972
	}
}
---


so you can delete or change save document as you want to facilitate your restore , in addition to not save for all the time the backup collection and have a huge collection i decide to keep the informatio using a ttl index in mongo shell i have run the following command 

```shell

db.bookbackup.createIndex({"_ts":1}, {expireAfterSeconds: 43200})

---

in my case i keep 12H00 . hope this article will help you 