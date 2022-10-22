import {PauseTriggerService} from './pause-trigger.service';
import {HttpClient} from "@angular/common/http";

describe('PauseTriggerService', () => {
  let service: PauseTriggerService;
  let httpClientSpy: jasmine.SpyObj<HttpClient>;

  beforeEach(() => {
    httpClientSpy = jasmine.createSpyObj('HttpClient', ['post']);
    service = new PauseTriggerService(httpClientSpy, 'http://localhost/')
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
