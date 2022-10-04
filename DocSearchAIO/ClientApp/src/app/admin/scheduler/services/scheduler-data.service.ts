import {Inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {map, Observable, of} from "rxjs";
import {environment} from "../../../../environments/environment";
import {catchError, take} from "rxjs/operators";
import {SchedulerStatisticResponseBase} from "../../config/interfaces/SchedulerStatisticResponse";
import {BaseError} from "../../config/interfaces/DocSearchConfiguration";
import {Either, makeLeft, makeRight} from "../../config/Either";

@Injectable({
  providedIn: 'root'
})
export class SchedulerDataService {
  baseUrl: string;

  private handleError(operation = 'operation') {
    return (error: any): Observable<Either<BaseError, SchedulerStatisticResponseBase>> => {
      console.error(error); // log to console instead
      const ret: BaseError = {
        errorMessage: error.message,
        errorCode: error.status,
        operation: operation
      }
      return of(makeLeft(ret));
    };
  }


  getSchedulerInfo(): Observable<Either<BaseError, SchedulerStatisticResponseBase>> {
    return this
      .httpClient
      .get<SchedulerStatisticResponseBase>(`${environment.apiUrl}api/administration/getSchedulerStatistics`)
      .pipe(
        take(1),
        map(result => makeRight(result)),
        catchError(this.handleError('getSchedulerInfo'))
      )
  }

  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }

}
