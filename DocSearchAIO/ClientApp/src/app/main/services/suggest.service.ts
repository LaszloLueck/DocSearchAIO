import {Inject, Injectable} from '@angular/core';
import {EMPTY, Observable, of, throwError} from "rxjs";
import {Suggest} from "../interfaces/Suggest";
import {HttpClient, HttpErrorResponse, HttpHeaders} from "@angular/common/http";
import {SuggestResponse} from "../interfaces/SuggestResponse";
import {SuggestRequest} from "../interfaces/SuggestRequest";
import {environment} from "../../../environments/environment";
import {catchError, take} from "rxjs/operators";

@Injectable({
  providedIn: 'root'
})
export class SuggestService {
  baseUrl: string;
  httpOptions = {
    headers: new HttpHeaders(
      {
        'Content-Type': 'application/json; charset=utf-8',
        'Cache-Control': 'no-cache'
      }
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


  getSuggest(suggest: SuggestRequest): Observable<SuggestResponse> {
    return this
      .httpClient
      .post<SuggestResponse>(`${environment.apiUrl}api/search/doSuggest`, suggest, this.httpOptions)
      .pipe(take(1),
        catchError(err => {
          console.log("An error occured");
          this.handleError(err)
          return EMPTY;
        })
      );
  }

  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }
}
