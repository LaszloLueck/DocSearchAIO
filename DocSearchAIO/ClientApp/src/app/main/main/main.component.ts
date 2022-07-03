import {Component, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";
import {NavigationResult, SearchResponse} from "../interfaces/SearchResponse";
import {DoSearchRequest} from "../interfaces/DoSearchRequest";
import {Observable, Subscription} from "rxjs";
import {SearchService} from "../services/search.service";
import {InitService} from "../services/init.service";
import {LocalStorageService} from "../../services/localStorageService";
import {LocalStorageDataset} from "../interfaces/LocalStorageDataset";
import {LocalStorageDefaultDataset} from "../interfaces/LocalStorageDefaultDataset";
import {SuggestionComponent} from "../components/suggestion/suggestion.component";

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit, OnDestroy {
  @ViewChild(SuggestionComponent, {static: true})
  private suggestionComponent!: SuggestionComponent;

  docCount!: number;
  searchResponse!: Observable<SearchResponse>;
  searchTerm!: string;
  closed: boolean = false;
  localStorageDataset!: LocalStorageDataset;
  localStorageSubscription!: Subscription;
  localStorageDatasetSubscription!: Subscription;


  constructor(private commonDataService: CommonDataService, private doSearchService: SearchService, private initService: InitService, private localStorageService: LocalStorageService) {
  }

  ngOnDestroy(): void {
    if (this.localStorageSubscription)
      this.localStorageSubscription.unsubscribe()

    if (this.localStorageDatasetSubscription)
      this.localStorageDatasetSubscription.unsubscribe();
  }

  ngOnInit(): void {
    this.commonDataService.sendData('Startseite');
    this.init();
  }

  init(): void {
    if (document.getElementById("searchField"))
      document.getElementById("searchField")!.focus();

    this.localStorageDatasetSubscription = this
      .localStorageService
      .getDataAsync()
      .subscribe(dataset => this.localStorageDataset = dataset);

    this.localStorageSubscription = this
      .initService
      .init(this.localStorageDataset)
      .subscribe(dataset => {
          this.localStorageService.setData(dataset);
        }
      );
  }

  getNgForCounter(count: number): number[] {
    return new Array(count);
  }

  handleExternalSearchParam(param: string) {
    this.searchTerm = param;

  }

  handleExternalSearch(eventHandler: any) {
    this.doSearch(0, this.localStorageDataset.itemsPerPage);
  }

  getCurrentPageNumber(navigation: NavigationResult): number {
    return navigation.currentPageSize === 0 ? 1 : Math.round(navigation.currentPage / navigation.currentPageSize + 1);
  }

  getModResult(navigation: NavigationResult): number {
    return (navigation.docCount % navigation.currentPageSize === 0) ? 0 : 1;
  }

  getPagingCount(navigation: NavigationResult): number {
    return navigation.docCount <= navigation.currentPageSize ? 0 : Math.round((navigation.docCount - navigation.docCount % navigation.currentPageSize) / navigation.currentPageSize) + this.getModResult(navigation);
  }

  cursorDown(): void {
    if (this.suggestionComponent && this.suggestionComponent.isOpen)
      this.suggestionComponent.cursorDown();
  }

  cursorUp(): void {
    if (this.suggestionComponent && this.suggestionComponent.isOpen)
      this.suggestionComponent.cursorUp();
  }

  pressEscape(): void {
    if (this.suggestionComponent)
      this.suggestionComponent.escapePressed();
  }

  keyPressed(arg: any): void {
    if (this.suggestionComponent) {
      const value: string = arg;
      this.suggestionComponent.keyPressed(value);
    }
  }

  doSearch(page: number, currentPageSize: number): void {
    this.suggestionComponent.escapePressed();
    const from = currentPageSize * page;
    if (!this.searchTerm || this.searchTerm.length == 0)
      this.searchTerm = '*';

    console.log('searchTerm: ' + this.searchTerm);
    console.log('searchPage: ' + from);
    const searchRequest: DoSearchRequest = {
      searchPhrase: this.searchTerm,
      from: from,
      size: this.localStorageDataset.itemsPerPage,
      filterWord: this.localStorageDataset.wordFilterActive && this.localStorageDataset.filterWord,
      filterExcel: this.localStorageDataset.excelFilterActive && this.localStorageDataset.filterExcel,
      filterPowerpoint: this.localStorageDataset.powerpointFilterActive && this.localStorageDataset.filterPowerpoint,
      filterPdf: this.localStorageDataset.pdfFilterActive && this.localStorageDataset.filterPdf,
      filterMsg: this.localStorageDataset.msgFilterActive && this.localStorageDataset.filterMsg,
      filterEml: this.localStorageDataset.emlFilterActive && this.localStorageDataset.filterEml
    }
    this.searchResponse = this
      .doSearchService
      .doSearch(searchRequest);
  }


}
