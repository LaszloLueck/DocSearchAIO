import {Inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {map, Observable, of} from "rxjs";
import {environment} from "../../../../environments/environment";
import {catchError, take} from "rxjs/operators";
import {SchedulerStatisticResponseBase} from "../../config/interfaces/SchedulerStatisticResponse";
import {BaseError} from "../../config/interfaces/DocSearchConfiguration";
import {Either, makeLeft, makeRight} from "../../../generic/either";
import {getErrorHandler} from "../../../generic/helper";

@Injectable({
  providedIn: 'root'
})
export class SchedulerDataService {
  baseUrl: string;

  getSchedulerInfo(): Observable<Either<BaseError, SchedulerStatisticResponseBase>> {
    return this
      .httpClient
      .get<SchedulerStatisticResponseBase>(`${environment.apiUrl}api/administration/getSchedulerStatistics`)
      .pipe(
        take(1),
        map(result => makeRight(result)),
        catchError(getErrorHandler<SchedulerStatisticResponseBase>('getSchedulerInfo'))
      )
  }

  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }

}
