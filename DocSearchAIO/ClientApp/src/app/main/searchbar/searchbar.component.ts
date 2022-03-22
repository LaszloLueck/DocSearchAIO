import {Component, EventEmitter, OnDestroy, OnInit, Output} from '@angular/core';
import {SearchService} from "./search.service";
import {DoSearchRequest} from "../interfaces/DoSearchRequest";
import {SearchResponse, SearchStatistic} from "../interfaces/SearchResponse";
import {catchError, take} from "rxjs/operators";
import {EMPTY, Observable, Subscription, tap} from "rxjs";

@Component({
  selector: 'app-searchbar',
  templateUrl: './searchbar.component.html',
  styleUrls: ['./searchbar.component.scss']
})
export class SearchbarComponent implements OnInit, OnDestroy {
  private searchResponse!: Subscription
  searchTerm!: string

  @Output() statistic: EventEmitter<SearchStatistic> = new EventEmitter<SearchStatistic>()

  constructor(private doSearchService: SearchService) {

  }

  ngOnDestroy(): void {
    this.searchResponse.unsubscribe()
  }

  ngOnInit(): void {
  }

  doSearch(){
    console.log(this.searchTerm);
    const searchRequest: DoSearchRequest = {
      searchPhrase: this.searchTerm,
      from: 0,
      size: 50,
      filterWord: true,
      filterExcel: true,
      filterPowerpoint: true,
      filterPdf: true,
      filterMsg: true,
      filterEml: true
    }
    this.searchResponse = this
      .doSearchService
      .doSearch(searchRequest)
      .pipe(
        take(1),
        tap(ewa => {
          console.log(ewa)
          this.statistic.emit(ewa.statistics);
        }),
        catchError(err => {
          console.error(err)
          return EMPTY
        })
      ).subscribe();
  }

}
