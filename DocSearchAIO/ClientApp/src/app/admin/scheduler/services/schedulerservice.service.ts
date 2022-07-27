import {Inject, Injectable} from '@angular/core';
import {HttpClient, HttpErrorResponse} from "@angular/common/http";
import {EMPTY, Observable, throwError} from "rxjs";
import {environment} from "../../../../environments/environment";
import {catchError, take} from "rxjs/operators";
import {SchedulerStatisticResponseBase} from "../../config/interfaces/SchedulerStatisticResponse";

@Injectable({
  providedIn: 'root'
})
export class SchedulerserviceService {
  baseUrl: string;

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


  getSchedulerInfo(): Observable<SchedulerStatisticResponseBase> {
    return this
      .httpClient
      .get<SchedulerStatisticResponseBase>(`${environment.apiUrl}api/administration/getSchedulerStatistics`)
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
