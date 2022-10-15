import { TestBed } from '@angular/core/testing';

import { ActionContentDataService } from './action-content-data.service';
import {SchedulerDataService} from "../../scheduler/services/scheduler-data.service";
import {HttpClient} from "@angular/common/http";

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
