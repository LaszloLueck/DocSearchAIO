import { TestBed } from '@angular/core/testing';

import { ResumeTriggerService } from './resume-trigger.service';
import {HttpClient} from "@angular/common/http";

describe('ResumeTriggerService', () => {
  let service: ResumeTriggerService;
  let httpClientSpy: jasmine.SpyObj<HttpClient>;

  beforeEach(() => {
    httpClientSpy = jasmine.createSpyObj('HttpClient', ['post']);
    service = new ResumeTriggerService(httpClientSpy, 'http://localhost/');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
