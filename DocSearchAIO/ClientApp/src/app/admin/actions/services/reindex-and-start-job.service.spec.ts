import { TestBed } from '@angular/core/testing';

import { ReindexAndStartJobService } from './reindex-and-start-job.service';
import {HttpClient, HttpErrorResponse} from "@angular/common/http";
import {JobRequest} from "../../config/interfaces/JobRequest";
import {asyncData, asyncError} from "../../../generic/helper";
import {isLeft, isRight, unwrapEither} from "../../../generic/either";

describe('ReindexAndStartJobService', () => {
  let service: ReindexAndStartJobService;
  let httpClientSpy: jasmine.SpyObj<HttpClient>;

  const request: JobRequest = {
    jobName: 'jobName',
    groupId: 'groupId'
  };

  beforeEach(() => {
    httpClientSpy = jasmine.createSpyObj('HttpClient', ['post']);
    service = new ReindexAndStartJobService(httpClientSpy, 'http://localhost/');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return an return result if the POST was successfully', (done: DoneFn) => {
    httpClientSpy
      .post
      .and
      .returnValue(asyncData(JSON.parse(responseBody)));

    service
      .reindexAndStartJob(request)
      .subscribe({
        next: either => {
          if (isLeft(either)) {
            done.fail('unwanted result');
          }
          if (isRight(either)) {
            const result = unwrapEither(either);
            expect(result.result).toBeTruthy();
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
      .post
      .and
      .returnValue(asyncError(response));

    service
      .reindexAndStartJob(request)
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

  const responseBody: string = `
    {
      "result": true
    }
  `;
});
