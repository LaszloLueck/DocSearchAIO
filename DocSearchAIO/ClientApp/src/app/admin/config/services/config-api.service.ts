import {Inject, Injectable} from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {map, Observable, of} from "rxjs";
import {BaseError, DocSearchConfiguration} from "../interfaces/DocSearchConfiguration"
import {catchError, take} from "rxjs/operators";
import {environment} from "../../../../environments/environment";
import {Either, makeLeft, makeRight} from "../../../generic/either";
import {getErrorHandler} from "../../../generic/helper";

@Injectable({
  providedIn: 'root'
})
export class ConfigApiService {
  baseUrl: string;

  httpOptions = {
  headers: new HttpHeaders({
    'Content-Type':  'application/json'
  })
  };

  getConfiguration(): Observable<Either<BaseError, DocSearchConfiguration>> {
    return this
      .httpClient
      .get<DocSearchConfiguration>(`${this.baseUrl}api/administration/getGenericContent`)
      .pipe(
        take(1),
        map(result => makeRight(result)),
        catchError(getErrorHandler<DocSearchConfiguration>('getConfiguration'))
      )
  }

  setConfiguration(document: DocSearchConfiguration) : Observable<boolean> {
    return this
      .httpClient
      .post<boolean>(`${environment.apiUrl}api/administration/setGenericContent`, document, this.httpOptions)
      .pipe(
        take(1),
        catchError(err => {
          getErrorHandler<DocSearchConfiguration>(err)
          return of(false);
        })
      )
  }

  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }
}
