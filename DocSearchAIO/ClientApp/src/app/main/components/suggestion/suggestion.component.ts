import {Component, EventEmitter, OnInit, Output, ViewChild} from '@angular/core';
import {SuggestService} from "../../services/suggest.service";
import {Observable} from "rxjs";
import {SuggestResponse} from "../../interfaces/SuggestResponse";
import {SuggestRequest} from "../../interfaces/SuggestRequest";
import {MainComponent} from "../../main/main.component";
import {LocalStorageDataset} from "../../interfaces/LocalStorageDataset";
import {LocalStorageService} from "../../../services/localStorageService";
import {LocalStorageDefaultDataset} from "../../interfaces/LocalStorageDefaultDataset";

@Component({
  selector: 'app-suggestion',
  templateUrl: './suggestion.component.html',
  styleUrls: ['./suggestion.component.scss']
})
export class SuggestionComponent implements OnInit {
  @ViewChild(MainComponent, {static: true})
  private mainComponent!: MainComponent;

  @Output() externalSearchParam: EventEmitter<string> = new EventEmitter<string>();
  @Output() doSearchEvent: EventEmitter<any> = new EventEmitter<any>();
  localStorageDataset!: LocalStorageDataset;

  results!: Observable<SuggestResponse>;
  private isOpen: boolean = false;
  toSearch!: string;
  minCountToSuggest: number = 3;

  constructor(private suggestService: SuggestService, private localStorageService: LocalStorageService) {
  }

  escapePressed(): void {
    console.log("ESCAPE pressed!");
    this.closeSuggest();
  }

  keyPressed(value: string): void {
    console.log("value is: " + value);
    this.toSearch = value;
    if (this.toSearch.length < this.minCountToSuggest && this.isOpen)
      this.closeSuggest();
    if (this.toSearch.length >= this.minCountToSuggest && !this.isOpen)
      this.openSuggest();
    if (this.toSearch.length >= this.minCountToSuggest)
      this.doSuggest();
  }

  ngOnInit(): void {
    this.isOpen = false;
    this.toSearch = "";

    if (!this.localStorageService.getData()) {
      this.localStorageDataset = new LocalStorageDefaultDataset();
    } else {
      this.localStorageDataset = this.localStorageService.getData() ?? new LocalStorageDefaultDataset();
    }
  }

  setToSearch(element: string): void {
    this.toSearch = element;
    this.externalSearchParam.emit(this.toSearch);
    this.doSearchEvent.emit();
    this.escapePressed();
  }

  hoverOver(element: MouseEvent){
    const foo = element.target as HTMLElement;
    foo.style.backgroundColor = 'dodgerblue';
    foo.style.color = 'white';
    const chs = foo.children[0].children as HTMLCollection;
    for (let chsKey in chs) {
      const el = chs[chsKey] as HTMLElement;
      if(el.style){

        el.style.color = 'white';
        el.style.backgroundColor = 'dodgerblue';
      }
    }

    //(document.querySelector('[id^="evenintheass_"]')! as HTMLElement).style.color = 'white';
  }

  hoverOut(element: MouseEvent){
    const foo = element.target as HTMLElement;
    foo.style.backgroundColor = 'white';
    foo.style.color = 'black';
    const chs = foo.children[0].children as HTMLCollection;
    for (let chsKey in chs) {
      const el = chs[chsKey] as HTMLElement;
      if(el.style) {
        el.style.color = 'dodgerblue';
        el.style.backgroundColor = 'white';
      }
    }

    //document.getElementById('evenintheass')!.style.color = 'dodgerblue';
    //(document.querySelector('[id^="evenintheass_"]')! as HTMLElement).style.color = 'dodgerblue';
  }

  cursorUp() {
    console.log("Cursor Up")
  }

  setFocus(){

  }

  cursorDown() {

  }

  doSuggest() {


    const request: SuggestRequest = {
      searchPhrase: this.toSearch,
      suggestWord: this.localStorageDataset.wordFilterActive,
      suggestExcel: this.localStorageDataset.excelFilterActive,
      suggestPowerpoint: this.localStorageDataset.powerpointFilterActive,
      suggestPdf: this.localStorageDataset.pdfFilterActive,
      suggestEml: this.localStorageDataset.emlFilterActive,
      suggestMsg: this.localStorageDataset.msgFilterActive
    };
    this.results = this
      .suggestService
      .getSuggest(request);
  }

  openSuggest() {
    if (!this.isOpen) {
      document.getElementById('search_suggest')!.classList.add("suggest-open");
      document.getElementById('search_suggest')!.classList.remove("suggest-close");
    }

    this.isOpen = true;
  }

  closeSuggest() {
    if (document.getElementById("search_suggest")) {
      document.getElementById("search_suggest")!.classList.add("suggest-close");
      document.getElementById("search_suggest")!.classList.remove("suggest-open");
    }
    this.isOpen = false;
  }

}
