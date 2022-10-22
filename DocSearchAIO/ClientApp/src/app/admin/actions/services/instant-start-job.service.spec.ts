import { TestBed } from '@angular/core/testing';

import { InstantStartJobService } from './instant-start-job.service';
import {HttpClient} from "@angular/common/http";

describe('InstantStartJobService', () => {
  let service: InstantStartJobService;
  let httpClientSpy: jasmine.SpyObj<HttpClient>;

  beforeEach(() => {
    httpClientSpy = jasmine.createSpyObj('HttpClient', ['post']);
    service = new InstantStartJobService(httpClientSpy, 'http://localhost/');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
