import {ConfigApiService} from './config-api.service';
import {HttpClient, HttpErrorResponse} from "@angular/common/http";
import {defer} from "rxjs";
import {BaseError} from "./interfaces/DocSearchConfiguration";
import {isLeft, isRight, unwrapEither} from "./Either";

describe('ConfigApiServiceService', () => {
  let service: ConfigApiService;
  let httpClientSpy: jasmine.SpyObj<HttpClient>;

  beforeEach(() => {
    httpClientSpy = jasmine.createSpyObj('HttpClient', ['get', 'post'])
    service = new ConfigApiService(httpClientSpy,"http://localhost/");
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return an error when the server returns a 500', (done: DoneFn) => {
    const response = new HttpErrorResponse({
      error: 'return a 500 error',
      status: 500,
      statusText: 'server error'
    });


    httpClientSpy.get.and.returnValue(asyncError(response));

    service
      .getConfiguration()
      .subscribe({
        next: either => {
          if(isRight(either)){
            done.fail("unwanted result");
          }
          if(isLeft(either)){
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

});

export function asyncData<T>(data: T) {
  return defer(() => Promise.resolve(data));
}

export function asyncError<T>(errorObject: any) {
  return defer(() => Promise.reject(errorObject));
}
