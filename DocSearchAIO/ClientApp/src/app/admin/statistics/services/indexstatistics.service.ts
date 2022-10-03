import {Inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {map, Observable, of} from "rxjs";
import {Either, makeLeft, makeRight} from "../../config/Either";
import {BaseError} from "../../config/interfaces/DocSearchConfiguration";
import {IndexStatisticsResponse} from "../../config/interfaces/IndexStatisticsResponse";
import {environment} from "../../../../environments/environment";
import {catchError, take} from "rxjs/operators";

@Injectable({
  providedIn: 'root'
})
export class IndexstatisticsService {
  baseUrl: string;

  private handleError(operation: string = 'operation') {
    return (error: any): Observable<Either<BaseError, IndexStatisticsResponse>> => {
      console.error(error);
      const ret: BaseError = {
        errorMessage: error.message,
        errorCode: error.status,
        operation: operation
      };
      return of(makeLeft(ret));
    }
  }


  getIndexcStatisticsData(): Observable<Either<BaseError, IndexStatisticsResponse>> {
    return this
      .httpClient
      .get<IndexStatisticsResponse>(`${environment.apiUrl}api/administration/getStatisticContentData`)
      .pipe(
        take(1),
        map(result => makeRight(result)),
        catchError(this.handleError('getIndexStatisticsData'))
      )
  }

  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }
}
