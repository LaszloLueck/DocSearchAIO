import {Component, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";
import {NavigationResult, SearchResponse} from "../interfaces/SearchResponse";
import {DoSearchRequest} from "../interfaces/DoSearchRequest";
import {Observable, Subscription} from "rxjs";
import {SearchService} from "../services/search.service";
import {NgbAlert} from "@ng-bootstrap/ng-bootstrap";

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit {
  docCount!: number;
  searchResponse!: Observable<SearchResponse>;
  searchTerm!: string;
  closed: boolean = false;

  constructor(private commonDataService: CommonDataService, private doSearchService: SearchService) { }


  ngOnInit(): void {
    this.commonDataService.sendData('Startseite');
  }

  getNgForCounter(count: number): number[] {
    return new Array(count);
  }

  getCurrentPageNumber(navigation: NavigationResult): number {
    return navigation.currentPageSize === 0 ? 1 : Math.round(navigation.currentPage / navigation.currentPageSize + 1);
  }

  getModResult(navigation: NavigationResult):number {
    return (navigation.docCount % navigation.currentPageSize === 0) ? 0 : 1;
  }

  getPagingCount(navigation: NavigationResult): number {
    return navigation.docCount <= navigation.currentPageSize ? 0 : Math.round((navigation.docCount - navigation.docCount % navigation.currentPageSize) / navigation.currentPageSize) + this.getModResult(navigation);
  }

  doSearch(page: number, currentPageSize: number): void{
    //const from = this.response?.searchResult ? this.response.searchResult.currentPageSize * page : 0;
    const from = currentPageSize * page;
    if(!this.searchTerm || this.searchTerm.length == 0)
      this.searchTerm = '*';

    console.log('searchTerm: ' + this.searchTerm);
    console.log('searchPage: ' + from);
    const searchRequest: DoSearchRequest = {
      searchPhrase: this.searchTerm,
      from: from,
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
      .doSearch(searchRequest);
  }


}
