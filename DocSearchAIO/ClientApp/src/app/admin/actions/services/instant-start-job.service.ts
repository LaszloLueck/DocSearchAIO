import {Inject, Injectable} from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {JobRequest} from "../../config/interfaces/JobRequest";
import {map, Observable} from "rxjs";
import {Either, makeRight} from "../../../generic/either";
import {BaseError} from "../../config/interfaces/DocSearchConfiguration";
import {JobResult} from "../../config/interfaces/JobResult";
import {environment} from "../../../../environments/environment";
import {catchError, take} from "rxjs/operators";
import {getErrorHandler} from "../../../generic/helper";

@Injectable({
  providedIn: 'root'
})
export class InstantStartJobService {

  private httpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  instantStartJob(jobData: JobRequest): Observable<Either<BaseError, JobResult>> {
    return this
      .httpClient
      .post<JobResult>(`${environment.apiUrl}api/administration/instandStartJob`, this.httpOptions)
      .pipe(
        take(1),
        map(result => makeRight(result)),
        catchError(getErrorHandler<JobResult>('instantStartJob'))
      )
  }



  constructor(private httpClient: HttpClient, @Inject('BASE_URL') private baseUrl: string) {
  }
}
