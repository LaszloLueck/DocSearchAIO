import { TestBed } from '@angular/core/testing';

import { ConfigApiService } from './config-api.service';
import {HttpClient} from "@angular/common/http";

describe('ConfigApiServiceService', () => {
  let service: ConfigApiService;

  beforeEach(() => {
    const httpClientSpy = jasmine.createSpyObj('HttpClient', ['post'])
    service = new ConfigApiService(httpClientSpy,"");
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
