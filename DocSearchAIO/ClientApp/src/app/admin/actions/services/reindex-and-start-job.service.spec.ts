import { TestBed } from '@angular/core/testing';

import { ReindexAndStartJobService } from './reindex-and-start-job.service';
import {HttpClient} from "@angular/common/http";

describe('ReindexAndStartJobService', () => {
  let service: ReindexAndStartJobService;
  let httpClientSpy: jasmine.SpyObj<HttpClient>;

  beforeEach(() => {
    httpClientSpy = jasmine.createSpyObj('HttpClient', ['post']);
    service = new ReindexAndStartJobService(httpClientSpy, 'http://localhost/');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
