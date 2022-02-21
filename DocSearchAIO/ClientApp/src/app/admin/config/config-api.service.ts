import {Inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {environment} from "../../../environments/environment";
import {DocSearchConfiguration} from "./interfaces/DocSearchConfiguration"

@Injectable({
  providedIn: 'root'
})
export class ConfigApiService {
  baseUrl: string;


  getConfiguration(): Observable<DocSearchConfiguration>{
    return this.httpClient.get<DocSearchConfiguration>(`${this.baseUrl}api/administration/getGenericContentData`)
  }

  constructor(private httpClient: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }
}
