import {Inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {map, Observable, of} from "rxjs";
import {Either, makeLeft, makeRight} from "../../../generic/either";
import {BaseError} from "../../config/interfaces/DocSearchConfiguration";
import {ActionContentData} from "../../config/interfaces/ActionContentData";
import {environment} from "../../../../environments/environment";
import {catchError, take} from "rxjs/operators";
import {getErrorHandler} from "../../../generic/helper";

@Injectable({
  providedIn: 'root'
})
export class ActionContentDataService {
  baseUrl: string;

  getActionData(): Observable<Either<BaseError, ActionContentData>> {
    return this
      .httpClient
      .get<ActionContentData>(`${environment.apiUrl}api/administration/getActionContentData`)
      .pipe(
        take(1),
        map(result => makeRight(result)),
        catchError(getErrorHandler<ActionContentData>('getActionData'))
      )
  }


  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }
}
