import {ActionContentDataService} from './action-content-data.service';
import {HttpClient, HttpErrorResponse} from "@angular/common/http";
import {asyncData, asyncError} from "../../../generic/helper";
import {isLeft, isRight, unwrapEither} from "../../../generic/either";

describe('ActionContentDataService', () => {
  let service: ActionContentDataService;
  let httpClientSpy: jasmine.SpyObj<HttpClient>;

  beforeEach(() => {
    httpClientSpy = jasmine.createSpyObj('HttpClient', ['get']);
    service = new ActionContentDataService(httpClientSpy, 'http://localhost/');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return an action-content-object when the server returns a result', (done: DoneFn) => {
    httpClientSpy
      .get
      .and
      .returnValue(asyncData(JSON.parse(responseBody)));

    service
      .getActionData()
      .subscribe({
        next: either => {
          if (isLeft(either)) {
            done.fail("unwanted result");
          }
          if (isRight(either)) {
            const result = unwrapEither(either);
            expect(result.docSearch_Processing.schedulerName).toBe("DocSearchScheduler");
            expect(result.docSearch_Cleanup.schedulerName).toBe('DocSearchScheduler');
            expect(result.docSearch_Processing.triggers).toHaveSize(6);
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

    httpClientSpy
      .get
      .and
      .returnValue(asyncError(response));

    service
      .getActionData()
      .subscribe({
        next: either => {
          if (isRight(either)) {
            done.fail('unwanted result');
          }

          if (isLeft(either)) {
            const result = unwrapEither(either);
            expect(result.errorCode).toBe(500);
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
  "docSearch_Processing": {
    "schedulerName": "DocSearchScheduler",
    "triggers": [
      {
        "triggerName": "emlProcessingTrigger",
        "groupName": "docSearch_Processing",
        "currentState": "Normal",
        "jobName": "emlProcessingJob",
        "jobState": 2
      },
      {
        "triggerName": "excelProcessingTrigger",
        "groupName": "docSearch_Processing",
        "currentState": "Paused",
        "jobName": "excelProcessingJob",
        "jobState": 2
      },
      {
        "triggerName": "msgProcessingTrigger",
        "groupName": "docSearch_Processing",
        "currentState": "Normal",
        "jobName": "msgProcessingJob",
        "jobState": 2
      },
      {
        "triggerName": "pdfProcessingTrigger",
        "groupName": "docSearch_Processing",
        "currentState": "Paused",
        "jobName": "pdfProcessingJob",
        "jobState": 2
      },
      {
        "triggerName": "powerpointProcessingTrigger",
        "groupName": "docSearch_Processing",
        "currentState": "Paused",
        "jobName": "powerpointProcessingJob",
        "jobState": 2
      },
      {
        "triggerName": "wordProcessingTrigger",
        "groupName": "docSearch_Processing",
        "currentState": "Paused",
        "jobName": "wordProcessingJob",
        "jobState": 2
      }
    ]
  },
  "docSearch_Cleanup": {
    "schedulerName": "DocSearchScheduler",
    "triggers": [
      {
        "triggerName": "emlCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "currentState": "Paused",
        "jobName": "emlCleanupJob",
        "jobState": 2
      },
      {
        "triggerName": "excelCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "currentState": "Paused",
        "jobName": "excelCleanupJob",
        "jobState": 2
      },
      {
        "triggerName": "msgCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "currentState": "Paused",
        "jobName": "msgCleanupJob",
        "jobState": 2
      },
      {
        "triggerName": "pdfCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "currentState": "Normal",
        "jobName": "pdfCleanupJob",
        "jobState": 2
      },
      {
        "triggerName": "powerpointCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "currentState": "Paused",
        "jobName": "powerpointCleanupJob",
        "jobState": 2
      },
      {
        "triggerName": "wordCleanupTrigger",
        "groupName": "docSearch_Cleanup",
        "currentState": "Paused",
        "jobName": "wordCleanupJob",
        "jobState": 2
      }
    ]
  }
}`;


});
