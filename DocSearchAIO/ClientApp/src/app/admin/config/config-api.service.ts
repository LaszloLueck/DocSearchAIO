import {Inject, Injectable} from '@angular/core';
import {HttpClient, HttpErrorResponse, HttpHeaders} from "@angular/common/http";
import {Observable, of, throwError} from "rxjs";
import {environment} from "../../../environments/environment";
import {DocSearchConfiguration} from "./interfaces/DocSearchConfiguration"
import {catchError, take} from "rxjs/operators";

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

  private handleError(error: HttpErrorResponse) {
    if (error.status === 0) {
      // A client-side or network error occurred. Handle it accordingly.
      console.error('An error occurred:', error.error);
    } else {
      // The backend returned an unsuccessful response code.
      // The response body may contain clues as to what went wrong.
      console.error(
        `Backend returned code ${error.status}, body was: `, error.error);
    }
    // Return an observable with a user-facing error message.
    return throwError(() => new Error('Something bad happened; please try again later.'));
  }

  getConfiguration(): Observable<DocSearchConfiguration>{
    return this.httpClient.get<DocSearchConfiguration>(`${this.baseUrl}api/administration/getGenericContentData`)
  }

  setConfiguration(document: DocSearchConfiguration) : Observable<boolean> {
    return this
      .httpClient
      .post<boolean>(`${this.baseUrl}api/administration/setGenericContent`, document, this.httpOptions)
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
