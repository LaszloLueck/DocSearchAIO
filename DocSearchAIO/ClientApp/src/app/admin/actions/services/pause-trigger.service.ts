import {Inject, Injectable} from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {TriggerRequest} from "../../config/interfaces/TriggerRequest";
import {map, Observable} from "rxjs";
import {Either, makeRight} from "../../../generic/either";
import {BaseError} from "../../config/interfaces/DocSearchConfiguration";
import {TriggerResult} from "../../config/interfaces/TriggerResult";
import {environment} from "../../../../environments/environment";
import {catchError, take} from "rxjs/operators";
import {getErrorHandler} from "../../../generic/helper";

@Injectable({
  providedIn: 'root'
})
export class PauseTriggerService {
  baseUrl: string;

  httpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  pauseTrigger(triggerData: TriggerRequest): Observable<Either<BaseError, TriggerResult>> {
    return this
      .httpClient
      .post<TriggerResult>(`${environment.apiUrl}api/administration/pauseTrigger`, this.httpOptions)
      .pipe(
        take(1),
        map(result => makeRight(result)),
        catchError(getErrorHandler<TriggerResult>('pauseTrigger'))
      )
  }


  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }
}
