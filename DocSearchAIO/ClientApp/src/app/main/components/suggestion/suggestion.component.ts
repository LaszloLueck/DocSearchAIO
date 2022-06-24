import {Component, EventEmitter, OnInit, Output, ViewChild} from '@angular/core';
import {SuggestService} from "../../services/suggest.service";
import {Observable, tap} from "rxjs";
import {Suggest} from "../../interfaces/Suggest";
import {SuggestResponse} from "../../interfaces/SuggestResponse";
import {SuggestRequest} from "../../interfaces/SuggestRequest";
import {MainComponent} from "../../main/main.component";

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
  results!: Observable<SuggestResponse>;
  private isOpen: boolean = false;
  toSearch!: string;
  minCountToSuggest: number = 3;

  constructor(private suggestService: SuggestService) {
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
  }

  setToSearch(element: string): void {
    this.toSearch = element;
    this.externalSearchParam.emit(this.toSearch);
    this.doSearchEvent.emit();
    this.escapePressed();
  }

  cursorUp() {
    console.log("Cursor Up")
  }

  setFocus(){

  }

  cursorDown() {

  }

  doSuggest() {
    const request: SuggestRequest = {searchPhrase: this.toSearch};
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
