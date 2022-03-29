import { TestBed } from '@angular/core/testing';

import { DocumentdetailService } from './documentdetail.service';

describe('DocumentdetailService', () => {
  let service: DocumentdetailService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(DocumentdetailService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
