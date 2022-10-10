import {IndexStatisticsService} from "./index-statistics.service";
import {HttpClient, HttpErrorResponse} from "@angular/common/http";
import {isLeft, isRight, unwrapEither} from "../../../generic/either";
import {asyncData, asyncError} from "../../../generic/helper";


describe('IndexStatisticsService', () => {
  let service: IndexStatisticsService;
  let httpClientSpy: jasmine.SpyObj<HttpClient>;

  beforeEach(() => {
    httpClientSpy = jasmine.createSpyObj('HttpClient', ['get']);
    service = new IndexStatisticsService(httpClientSpy, 'http://localhost/');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return a responseobject when the server returns a result', (done: DoneFn) => {
    httpClientSpy
      .get
      .and
      .returnValue(asyncData(JSON.parse(responseBody)));

    service
      .getIndexcStatisticsData()
      .subscribe({
        next: either => {
          if(isLeft(either)) {
            done.fail("unwanted result");
          }
          if(isRight(either)) {
            const result = unwrapEither(either);
            expect(result.entireDocCount).toBe(1365);
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

    httpClientSpy.get.and.returnValue(asyncError(response));

    service
      .getIndexcStatisticsData()
      .subscribe({
        next: either => {
          if(isRight(either)) {
            done.fail('unwanted result');
          }
          if(isLeft(either)) {
            const foo = unwrapEither(either);
            expect(foo.errorCode).toBe(500);
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
  "indexStatisticModels": [
    {
      "indexName": "officedocuments-excel",
      "docCount": 150,
      "sizeInBytes": 10770876,
      "fetchTimeMs": 391,
      "fetchTotal": 23,
      "queryTimeMs": 29,
      "queryTotal": 5,
      "suggestTimeMs": 182,
      "suggestTotal": 23
    },
    {
      "indexName": "officedocuments-pdf",
      "docCount": 144,
      "sizeInBytes": 6985610,
      "fetchTimeMs": 174,
      "fetchTotal": 23,
      "queryTimeMs": 19,
      "queryTotal": 5,
      "suggestTimeMs": 52,
      "suggestTotal": 23
    },
    {
      "indexName": "officedocuments-powerpoint",
      "docCount": 381,
      "sizeInBytes": 9487256,
      "fetchTimeMs": 134,
      "fetchTotal": 23,
      "queryTimeMs": 18,
      "queryTotal": 5,
      "suggestTimeMs": 63,
      "suggestTotal": 23
    },
    {
      "indexName": "officedocuments-word",
      "docCount": 690,
      "sizeInBytes": 36990189,
      "fetchTimeMs": 1737,
      "fetchTotal": 45,
      "queryTimeMs": 127,
      "queryTotal": 22,
      "suggestTimeMs": 279,
      "suggestTotal": 23
    }
  ],
  "runtimeStatistics": [],
  "entireDocCount": 1365,
  "entireSizeInBytes": 64233931
}`;

});
