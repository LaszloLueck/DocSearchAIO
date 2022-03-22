import {Inject, Injectable} from '@angular/core';
import {HttpClient, HttpErrorResponse, HttpHeaders} from "@angular/common/http";
import {EMPTY, Observable, of, throwError} from "rxjs";
import {DoSearchRequest} from "../interfaces/DoSearchRequest";
import {SearchResponse} from "../interfaces/SearchResponse";
import {catchError, take} from "rxjs/operators";

@Injectable({
  providedIn: 'root'
})
export class SearchService {
  baseUrl: string;
  httpOptions = {
    headers: new HttpHeaders({'Content-Type': 'application/json'})
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

  doSearch(searchRequest: DoSearchRequest): Observable<SearchResponse> {
    return this
      .httpClient
      .post<SearchResponse>(`${this.baseUrl}api/search/doSearch`, searchRequest, this.httpOptions)
      .pipe(
        catchError(err => {
          this.handleError(err)
          return EMPTY;
        })
      )
  }

  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }
}
