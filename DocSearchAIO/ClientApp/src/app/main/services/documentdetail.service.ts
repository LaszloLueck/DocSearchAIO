import {Inject, Injectable} from '@angular/core';
import {HttpClient, HttpErrorResponse, HttpHeaders} from "@angular/common/http";
import {EMPTY, Observable, throwError} from "rxjs";
import {B} from "@angular/cdk/keycodes";
import {DocumentDetailRequest} from "../interfaces/DocumentDetailRequest";
import {DocumentDetailResponse} from "../interfaces/DocumentDetailResponse";
import {environment} from "../../../environments/environment";
import {catchError, take} from "rxjs/operators";

@Injectable({
  providedIn: 'root'
})
export class DocumentdetailService {
  baseUrl: string;
  httpOptions = {
    headers: new HttpHeaders(
      {
        'Content-Type': 'application/json; charset=utf-8',
        'Cache-Control': 'no-cache'
      }
    )
  }
  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  documentDetail(documentDetailRequest: DocumentDetailRequest): Observable<DocumentDetailResponse>{
    return this
      .httpClient
      .post<DocumentDetailResponse>(`${environment.apiUrl}api/search/documentDetailData`, documentDetailRequest, this.httpOptions)
      .pipe(
        take(1),
        catchError(err => {
          console.log("An error occured");
          this.handleError(err)
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
