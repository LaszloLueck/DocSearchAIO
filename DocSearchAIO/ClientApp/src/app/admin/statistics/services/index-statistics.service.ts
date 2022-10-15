import {Inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {map, Observable, of} from "rxjs";
import {Either, makeLeft, makeRight} from "../../../generic/either";
import {BaseError} from "../../config/interfaces/DocSearchConfiguration";
import {IndexStatisticsResponse} from "../../config/interfaces/IndexStatisticsResponse";
import {environment} from "../../../../environments/environment";
import {catchError, take} from "rxjs/operators";
import {getErrorHandler} from "../../../generic/helper";

@Injectable({
  providedIn: 'root'
})
export class IndexStatisticsService {
  baseUrl: string;

  getIndexStatisticsData(): Observable<Either<BaseError, IndexStatisticsResponse>> {
    return this
      .httpClient
      .get<IndexStatisticsResponse>(`${environment.apiUrl}api/administration/getStatisticContentData`)
      .pipe(
        take(1),
        map(result => makeRight(result)),
        catchError(getErrorHandler<IndexStatisticsResponse>('getIndexStatisticsData'))
      )
  }

  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }
}
