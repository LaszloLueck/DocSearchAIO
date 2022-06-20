import { TestBed } from '@angular/core/testing';

import { SuggestService } from './suggest.service';

describe('SuggestService', () => {
  let service: SuggestService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SuggestService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
