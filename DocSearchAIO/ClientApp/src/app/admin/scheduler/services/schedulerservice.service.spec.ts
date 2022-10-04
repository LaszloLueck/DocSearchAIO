import {SchedulerDataService} from "./scheduler-data.service";
import {HttpClient, HttpErrorResponse} from "@angular/common/http";
import {asyncData, asyncError} from "../../config/config-api.service.spec";
import {isLeft, isRight, unwrapEither} from "../../config/Either";


describe('SchedulerService', () => {
  let service: SchedulerDataService;
  let httpClientSpy: jasmine.SpyObj<HttpClient>;

  beforeEach(() => {
    httpClientSpy = jasmine.createSpyObj('HttpClient', ['get']);
    service = new SchedulerDataService(httpClientSpy, 'http://localhost/');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return an schedulerinfo object when the server returns a result', (done: DoneFn) => {
    httpClientSpy
      .get
      .and
      .returnValue(asyncData(JSON.parse(responseBody)));

    service
      .getSchedulerInfo()
      .subscribe({
        next: either => {
          if(isLeft(either)) {
            done.fail("unwanted result");
          }

          if(isRight(either)) {
            const result = unwrapEither(either);
            expect(result.docSearch_Processing.schedulerName).toBe("DocSearchScheduler");
          }
          done();
        },
        error: error => {
          expect(error.error).toContain('return a 500 error');
          done();
        }
      });


  })

  it('should return an error when the server returns a 500', (done: DoneFn) => {
    const response = new HttpErrorResponse({
      error: 'return a 500 error',
      status: 500,
      statusText: 'server error'
    });

    httpClientSpy
      .get
      .and
      .returnValue(asyncError(response));

    service
      .getSchedulerInfo()
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
      });

  });


  const responseBody: string = `{
  "docSearch_Processing": {
    "schedulerName": "DocSearchScheduler",
    "schedulerInstanceId": "DocSearchScheduler_001",
    "state": "Gestartet",
    "triggerElements": [
      {
        "triggerName": "emlProcessingTrigger",
        "groupName": "docSearch_Processing",
        "nextFireTime": "2022-10-04T18:13:34+02:00",
        "startTime": "2022-10-04T17:13:34+02:00",
        "lastFireTime": "2022-10-04T17:13:34+02:00",
        "triggerState": "Normal",
        "description": "trigger for EmlElasticDocument-processing and indexing",
        "processingState": false,
        "jobName": "emlProcessingJob"
      },
      {
        "triggerName": "excelProcessingTrigger",
        "groupName": "docSearch_Processing",
        "nextFireTime": "2022-10-04T18:13:32+02:00",
        "startTime": "2022-10-04T17:13:32+02:00",
        "lastFireTime": "2022-10-04T17:13:32+02:00",
        "triggerState": "Paused",
        "description": "trigger for ExcelElasticDocument-processing and indexing",
        "processingState": false,
        "jobName": "excelProcessingJob"
      },
      {
        "triggerName": "msgProcessingTrigger",
        "groupName": "docSearch_Processing",
        "nextFireTime": "2022-10-04T18:13:33+02:00",
        "startTime": "2022-10-04T17:13:33+02:00",
        "lastFireTime": "2022-10-04T17:13:33+02:00",
        "triggerState": "Normal",
        "description": "trigger for MsgElasticDocument-processing and indexing",
        "processingState": false,
        "jobName": "msgProcessingJob"
      },
      {
        "triggerName": "pdfProcessingTrigger",
        "groupName": "docSearch_Processing",
        "nextFireTime": "2022-10-04T18:13:36+02:00",
        "startTime": "2022-10-04T17:13:36+02:00",
        "lastFireTime": "2022-10-04T17:13:36+02:00",
        "triggerState": "Paused",
        "description": "trigger for PdfElasticDocument-processing and indexing",
        "processingState": false,
        "jobName": "pdfProcessingJob"
      },
      {
        "triggerName": "powerpointProcessingTrigger",
        "groupName": "docSearch_Processing",
        "nextFireTime": "2022-10-04T18:13:34+02:00",
        "startTime": "2022-10-04T17:13:34+02:00",
        "lastFireTime": "2022-10-04T17:13:34+02:00",
        "triggerState": "Paused",
        "description": "trigger for PowerpointElasticDocument-processing and indexing",
        "processingState": false,
        "jobName": "powerpointProcessingJob"
      },
      {
        "triggerName": "wordProcessingTrigger",
        "groupName": "docSearch_Processing",
        "nextFireTime": "2022-10-04T18:13:30+02:00",
        "startTime": "2022-10-04T17:13:30+02:00",
        "lastFireTime": "2022-10-04T17:13:30+02:00",
        "triggerState": "Paused",
        "description": "trigger for WordElasticDocument-processing and indexing",
        "processingState": false,
        "jobName": "wordProcessingJob"
      }
    ]
  },
  "docSearch_Cleanup": {
    "schedulerName": "DocSearchScheduler",
    "schedulerInstanceId": "DocSearchScheduler_001",
    "state": "Gestartet",
    "triggerElements": [
      {
        "triggerName": "emlCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "nextFireTime": "2022-10-04T17:43:44+02:00",
        "startTime": "2022-10-04T17:13:44+02:00",
        "lastFireTime": "2022-10-04T17:13:44+02:00",
        "triggerState": "Paused",
        "description": "trigger for EmlCleanupDocument-processing and indexing",
        "processingState": false,
        "jobName": "emlCleanupJob"
      },
      {
        "triggerName": "excelCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "nextFireTime": "2022-10-04T17:43:44+02:00",
        "startTime": "2022-10-04T17:13:44+02:00",
        "lastFireTime": "2022-10-04T17:13:44+02:00",
        "triggerState": "Paused",
        "description": "trigger for ExcelCleanupDocument-processing and indexing",
        "processingState": false,
        "jobName": "excelCleanupJob"
      },
      {
        "triggerName": "msgCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "nextFireTime": "2022-10-04T17:43:44+02:00",
        "startTime": "2022-10-04T17:13:44+02:00",
        "lastFireTime": "2022-10-04T17:13:44+02:00",
        "triggerState": "Paused",
        "description": "trigger for MsgCleanupDocument-processing and indexing",
        "processingState": false,
        "jobName": "msgCleanupJob"
      },
      {
        "triggerName": "pdfCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "nextFireTime": "2022-10-04T17:43:44+02:00",
        "startTime": "2022-10-04T17:13:44+02:00",
        "lastFireTime": "2022-10-04T17:13:44+02:00",
        "triggerState": "Normal",
        "description": "trigger for PdfCleanupDocument-processing and indexing",
        "processingState": false,
        "jobName": "pdfCleanupJob"
      },
      {
        "triggerName": "powerpointCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "nextFireTime": "2022-10-04T17:43:44+02:00",
        "startTime": "2022-10-04T17:13:44+02:00",
        "lastFireTime": "2022-10-04T17:13:44+02:00",
        "triggerState": "Paused",
        "description": "trigger for PowerpointCleanupDocument-processing and indexing",
        "processingState": false,
        "jobName": "powerpointCleanupJob"
      },
      {
        "triggerName": "wordCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "nextFireTime": "2022-10-04T17:43:44+02:00",
        "startTime": "2022-10-04T17:13:44+02:00",
        "lastFireTime": "2022-10-04T17:13:44+02:00",
        "triggerState": "Paused",
        "description": "trigger for WordCleanupDocument-processing and indexing",
        "processingState": false,
        "jobName": "wordCleanupJob"
      }
    ]
  }
}`;

});
