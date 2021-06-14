using System.Collections.Generic;
using System.Text.Json;
using DocSearchAIO.DocSearch.TOs;
using NUnit.Framework;

namespace DocSearchAIO_Test
{
    public class ConverterTests
    {
        [Test]
        public void ConvertGenericAdminJson()
        {
            var json = @"{""scanPath"":""d:\\temp\\00-Gesamtprojekt\\"",""indexName"":""officedocuments"",""schedulerName"":""DocSearchScheduler"",""actorSystemName"":""actorSystem"",""groupName"":""docSearch_Processing"",""schedulerId"":""DocSearchScheduler_001"",""elasticEndpoints"":[""http://127.0.0.1:9200""],""uriReplacement"":""https://risprepository:8800/svns/PNR/extern"",""processorConfigurations"":{{""PdfElasticDocument"":{""runsEvery"":3600,""startDelay"":7,""parallelism"":2,""fileExtension"":""*.pdf"",""excludeFilter"":"""",""jobName"":""pdfProcessingJob"",""triggerName"":""pdfProcessingTrigger"",""indexSuffix"":""pdf""}},{""PowerpointElasticDocument"":{""runsEvery"":3600,""startDelay"":5,""parallelism"":2,""fileExtension"":""*.pptx"",""excludeFilter"":""~$"",""jobName"":""powerpointProcessingJob"",""triggerName"":""powerpointProcessingTrigger"",""indexSuffix"":""powerpoint""}},{""WordElasticDocument"":{""runsEvery"":3600,""startDelay"":1,""parallelism"":2,""fileExtension"":""*.docx"",""excludeFilter"":""~$"",""jobName"":""wordProcessingJob"",""triggerName"":""wordProcessingTrigger"",""indexSuffix"":""word""}}}}";

            var foo = new AdministrationGenericModel()
            {
                ActorSystemName = "actorsystemname",
                ElasticEndpoints = new List<string>() {"1", "2"},
                GroupName = "groupName",
                IndexName = "indexName",
                ProcessorConfigurations = new Dictionary<string, AdministrationGenericModel.ProcessorConfiguration>()
                {
                    {
                        "key",
                        new AdministrationGenericModel.ProcessorConfiguration()
                        {
                            Parallelism = 1, ExcludeFilter = "excludeFilter", FileExtension = "fileExtension",
                            IndexSuffix = "indexSuffix", JobName = "jobName", RunsEvery = 1, StartDelay = 1,
                            TriggerName = "triggerName"
                        }
                    }
                },
                ScanPath = "scanPath",
                SchedulerId = "schedulerId",
                SchedulerName = "schedulerName",
                UriReplacement = "uriReplacement"
            };

            var j = JsonSerializer.Serialize(foo, new JsonSerializerOptions(){WriteIndented = true});
            var dic = JsonSerializer.Deserialize<AdministrationGenericModel>(json);
            
            Assert.NotNull(dic);
            Assert.Equals("officedocuments", dic.IndexName);


        }
        
    }
}