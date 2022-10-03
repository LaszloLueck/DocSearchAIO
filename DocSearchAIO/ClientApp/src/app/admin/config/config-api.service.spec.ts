import {ConfigApiService} from './config-api.service';
import {HttpClient, HttpErrorResponse} from "@angular/common/http";
import {defer} from "rxjs";
import {isLeft, isRight, unwrapEither} from "./Either";

describe('ConfigApiServiceService', () => {
  let service: ConfigApiService;
  let httpClientSpy: jasmine.SpyObj<HttpClient>;

  beforeEach(() => {
    httpClientSpy = jasmine.createSpyObj('HttpClient', ['get', 'post']);
    service = new ConfigApiService(httpClientSpy, "http://localhost/");
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return an configuration object when the server returns a result', (done: DoneFn) => {
    httpClientSpy
      .get
      .and
      .returnValue(asyncData(JSON.parse(responseBody)));

    service
      .getConfiguration()
      .subscribe({
        next: either => {
          if(isLeft(either)) {
            done.fail("unwanted result");
          }

          if(isRight(either)) {
            const result = unwrapEither(either);
            expect(result.indexName).toBe("officedocuments");
          }
          done();
        },
        error: error => {
          expect(error.error).toContain('return a 500 error');
          done();
        }
      })
  });

  it('should return an error when the server returns a 500', (done: DoneFn) => {
    const response = new HttpErrorResponse({
      error: 'return a 500 error',
      status: 500,
      statusText: 'server error'
    });


    httpClientSpy.get.and.returnValue(asyncError(response));

    service
      .getConfiguration()
      .subscribe({
        next: either => {
          if (isRight(either)) {
            done.fail("unwanted result");
          }
          if (isLeft(either)) {
            const foo = unwrapEither(either);
            expect(foo.errorCode).toBe(500);
          }
          done();
        },
        error: error => {
          expect(error.error).toContain('return a 500 error');
          done();
        }
      })

  });


  const responseBody: string = `{
  "scanPath": "/Users/laszlo/Documents/bva/02_Chefdesign",
  "elasticEndpoints": [
    "http://127.0.0.1:9200"
  ],
  "indexName": "officedocuments",
  "elasticUser": "elastic",
  "elasticPassword": "8USt79lz2UEkPs3q",
  "schedulerName": "DocSearchScheduler",
  "schedulerId": "DocSearchScheduler_001",
  "actorSystemName": "actorSystem",
  "processorGroupName": "docSearch_Processing",
  "cleanupGroupName": "docSearch_Cleanup",
  "uriReplacement": "https://risprepository:8800/svns/PNR/extern/",
  "comparerDirectory": "./Resources/comparer",
  "statisticsDirectory": "./Resources/statistics",
  "processorConfigurations": {
    "EmlElasticDocument": {
      "parallelism": 10,
      "startDelay": 5,
      "runsEvery": 3600,
      "excludeFilter": "",
      "indexSuffix": "eml",
      "fileExtension": "*.eml",
      "jobName": "emlProcessingJob",
      "triggerName": "emlProcessingTrigger"
    },
    "ExcelElasticDocument": {
      "parallelism": 10,
      "startDelay": 3,
      "runsEvery": 3600,
      "excludeFilter": "~$",
      "indexSuffix": "excel",
      "fileExtension": "*.xlsx",
      "jobName": "excelProcessingJob",
      "triggerName": "excelProcessingTrigger"
    },
    "MsgElasticDocument": {
      "parallelism": 10,
      "startDelay": 4,
      "runsEvery": 3600,
      "excludeFilter": "",
      "indexSuffix": "msg",
      "fileExtension": "*.msg",
      "jobName": "msgProcessingJob",
      "triggerName": "msgProcessingTrigger"
    },
    "PdfElasticDocument": {
      "parallelism": 10,
      "startDelay": 7,
      "runsEvery": 3600,
      "excludeFilter": "",
      "indexSuffix": "pdf",
      "fileExtension": "*.pdf",
      "jobName": "pdfProcessingJob",
      "triggerName": "pdfProcessingTrigger"
    },
    "PowerpointElasticDocument": {
      "parallelism": 10,
      "startDelay": 5,
      "runsEvery": 3600,
      "excludeFilter": "~$",
      "indexSuffix": "powerpoint",
      "fileExtension": "*.pptx",
      "jobName": "powerpointProcessingJob",
      "triggerName": "powerpointProcessingTrigger"
    },
    "WordElasticDocument": {
      "parallelism": 10,
      "startDelay": 1,
      "runsEvery": 3600,
      "excludeFilter": "~$",
      "indexSuffix": "word",
      "fileExtension": "*.docx",
      "jobName": "wordProcessingJob",
      "triggerName": "wordProcessingTrigger"
    }
  },
  "cleanupConfigurations": {
    "EmlCleanupDocument": {
      "forComparer": "ComparerModelEml",
      "forIndexSuffix": "eml",
      "startDelay": 15,
      "runsEvery": 1800,
      "parallelism": 5,
      "jobName": "emlCleanupJob",
      "triggerName": "emlCleanupTrigger"
    },
    "ExcelCleanupDocument": {
      "forComparer": "ComparerModelExcel",
      "forIndexSuffix": "excel",
      "startDelay": 15,
      "runsEvery": 1800,
      "parallelism": 5,
      "jobName": "excelCleanupJob",
      "triggerName": "excelCleanupTrigger"
    },
    "MsgCleanupDocument": {
      "forComparer": "ComparerModelMsg",
      "forIndexSuffix": "msg",
      "startDelay": 15,
      "runsEvery": 1800,
      "parallelism": 5,
      "jobName": "msgCleanupJob",
      "triggerName": "msgCleanupTrigger"
    },
    "PdfCleanupDocument": {
      "forComparer": "ComparerModelPdf",
      "forIndexSuffix": "pdf",
      "startDelay": 15,
      "runsEvery": 1800,
      "parallelism": 5,
      "jobName": "pdfCleanupJob",
      "triggerName": "pdfCleanupTrigger"
    },
    "PowerpointCleanupDocument": {
      "forComparer": "ComparerModelPowerpoint",
      "forIndexSuffix": "powerpoint",
      "startDelay": 15,
      "runsEvery": 1800,
      "parallelism": 5,
      "jobName": "powerpointCleanupJob",
      "triggerName": "powerpointCleanupTrigger"
    },
    "WordCleanupDocument": {
      "forComparer": "ComparerModelWord",
      "forIndexSuffix": "word",
      "startDelay": 15,
      "runsEvery": 1800,
      "parallelism": 5,
      "jobName": "wordCleanupJob",
      "triggerName": "wordCleanupTrigger"
    }
  }
}`;

});

export function asyncData<T>(data: T) {
  return defer(() => Promise.resolve(data));
}

export function asyncError<T>(errorObject: any) {
  return defer(() => Promise.reject(errorObject));
}
