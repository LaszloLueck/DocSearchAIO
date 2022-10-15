import {defer, Observable, of} from "rxjs";
import {Either, makeLeft} from "./either";
import {BaseError} from "../admin/config/interfaces/DocSearchConfiguration";


export function asyncData<T>(data: T) {
  return defer(() => Promise.resolve(data));
}

export function asyncError<T>(errorObject: any) {
  return defer(() => Promise.reject(errorObject));
}

export function getErrorHandler<T>(operation: string = 'operation') {
  return (error: any): Observable<Either<BaseError, T>> => {
    console.error(error); // log to console instead
    const ret: BaseError = {
      errorMessage: error.message,
      errorCode: error.status,
      operation: operation
    }
    return of(makeLeft(ret));
  };

}
