import { Injectable } from '@angular/core';
import {HttpClient, HttpErrorResponse, HttpHeaders} from "@angular/common/http";
import {LocalStorageDataset} from "../interfaces/LocalStorageDataset";
import {EMPTY, Observable, throwError} from "rxjs";
import {environment} from "../../../environments/environment";
import {catchError, take} from "rxjs/operators";

@Injectable({
  providedIn: 'root'
})
export class InitService {
  httpOptions = {
    headers: new HttpHeaders(
      {
        'Content-Type': 'application/json; charset=utf-8',
        'Cache-Control': 'no-cache'
      }
    )
  }
  constructor(private httpClient: HttpClient) { }

  init(localStorageDataSet: LocalStorageDataset): Observable<LocalStorageDataset>{
    return this
      .httpClient
      .post<LocalStorageDataset>(`${environment.apiUrl}api/base/init`, localStorageDataSet, this.httpOptions)
      .pipe(
        take(1),
        catchError(err => {
          console.log("An error occured");
          this.handleError(err);
          return EMPTY;
        })
      )
  }

  private handleError(error: HttpErrorResponse){
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

}
