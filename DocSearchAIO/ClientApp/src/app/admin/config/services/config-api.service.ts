import {Inject, Injectable} from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {map, Observable, of} from "rxjs";
import {BaseError, DocSearchConfiguration} from "../interfaces/DocSearchConfiguration"
import {catchError, take} from "rxjs/operators";
import {environment} from "../../../../environments/environment";
import {Either, makeLeft, makeRight} from "../../../generic/either";

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

  private handleError(operation = 'operation') {
    return (error: any): Observable<Either<BaseError, DocSearchConfiguration>> => {
      console.error(error); // log to console instead
      const ret: BaseError = {
        errorMessage: error.message,
        errorCode: error.status,
        operation: operation
      }
      return of(makeLeft(ret));
    };
  }

  getConfiguration(): Observable<Either<BaseError, DocSearchConfiguration>> {
    return this
      .httpClient
      .get<DocSearchConfiguration>(`${this.baseUrl}api/administration/getGenericContent`)
      .pipe(
        take(1),
        map(result => makeRight(result)),
        catchError(this.handleError('getConfiguration'))
      )
  }

  setConfiguration(document: DocSearchConfiguration) : Observable<boolean> {
    return this
      .httpClient
      .post<boolean>(`${environment.apiUrl}api/administration/setGenericContent`, document, this.httpOptions)
      .pipe(
        take(1),
        catchError(err => {
          this.handleError(err)
          return of(false);
        })
      )
  }

  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }
}
