import {Component, EventEmitter, OnDestroy, OnInit, Output, ViewChild} from '@angular/core';
import {SuggestService} from "../../services/suggest.service";
import {Subscription} from "rxjs";
import {SuggestResponse} from "../../interfaces/SuggestResponse";
import {SuggestRequest} from "../../interfaces/SuggestRequest";
import {MainComponent} from "../../main/main.component";
import {LocalStorageDataset} from "../../interfaces/LocalStorageDataset";
import {LocalStorageService} from "../../../services/LocalStorageService";

@Component({
  selector: 'app-suggestion',
  templateUrl: './suggestion.component.html',
  styleUrls: ['./suggestion.component.scss']
})
export class SuggestionComponent implements OnInit, OnDestroy {
  @ViewChild(MainComponent, {static: true})
  private mainComponent!: MainComponent;

  @Output() externalSearchParam: EventEmitter<string> = new EventEmitter<string>();
  @Output() doSearchEvent: EventEmitter<any> = new EventEmitter<any>();
  localStorageDataset!: LocalStorageDataset;
  localStorageDatasetSubscription!: Subscription;

  responses!: SuggestResponse;
  sub!: Subscription;

  isOpen: boolean = false;
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

    this.localStorageDatasetSubscription = this.localStorageService
      .getDataAsync()
      .subscribe(data => this.localStorageDataset = data);
  }

  ngOnDestroy(): void {
    if (this.sub)
      this.sub.unsubscribe();

    if (this.localStorageDatasetSubscription)
      this.localStorageDatasetSubscription.unsubscribe();
  }


  setToSearch(element: string): void {
    this.toSearch = element;
    this.externalSearchParam.emit(this.toSearch);
    this.doSearchEvent.emit();
    this.escapePressed();
  }

  prepareHoverOver(mouseEvent: MouseEvent) {
    const element = mouseEvent.target as HTMLElement;
    this.hoverOver(element);
  }


  prepareHoverOut(mouseEvent: MouseEvent) {
    const element = mouseEvent.target as HTMLElement;
    this.hoverOut(element);
  }


  hoverOver(element: HTMLElement) {
    element.style.backgroundColor = element.style.color;
    element.style.borderColor = element.style.color;
    element.setAttribute("selectedValue", element.style.color);
    element.style.color = 'white';
    console.log(element);
  }

  setStyle(id: number): string {
    if (this.responses && this.localStorageDataset) {
      const current = this.responses.suggests[id];
      let currentStyle = '';
      for (var i = 0; i < current.indexNames.length; i++) {
        if (current.indexNames[i] === 'officedocuments-word' && this.localStorageDataset.filterWord) {
          currentStyle = "color: #28a745; text-align: left";
          break;
        }
        if (current.indexNames[i] === 'officedocuments-excel' && this.localStorageDataset.filterExcel) {
          currentStyle = "color: #28a745; text-align: left";
          break;
        }
        if (current.indexNames[i] === 'officedocuments-powerpoint' && this.localStorageDataset.filterPowerpoint) {
          currentStyle = "color: #28a745; text-align: left";
          break;
        }
        if (current.indexNames[i] === 'officedocuments-pdf' && this.localStorageDataset.filterPdf) {
          currentStyle = "color: #28a745; text-align: left";
          break;
        }
        if (current.indexNames[i] === 'officedocuments-msg' && this.localStorageDataset.filterMsg) {
          currentStyle = "color: #28a745; text-align: left";
          break;
        }
        if (current.indexNames[i] === 'officedocuments-eml' && this.localStorageDataset.filterEml) {
          currentStyle = "color: #28a745; text-align: left";
          break;
        }
      }

      if (currentStyle.length === 0)
        currentStyle = "color: #dc3545; text-align: left";

      return currentStyle;
    }

    return "";
  }

  hoverOut(element: HTMLElement) {
    element.style.color = element.getAttribute("selectedValue") ?? "#ffffff";
    element.removeAttribute("selectedValue");
    element.style.backgroundColor = "";
    element.style.borderColor = "";
  }

  cursorUp() {
    console.log("Cursor Up");
    const elements = document.querySelectorAll('[id^="suggestitem_"]');
    if (elements && elements.length > 0) {
      const current = Array.from(elements).filter(element => element.attributes.getNamedItem('labelSelected'));
      if (!current || current.length == 0) {
        var maxValue = elements.length - 1;
        elements[maxValue].setAttribute("labelSelected", "true");
        elements[maxValue].scrollIntoView(false);
        this.toSearch = (elements[maxValue] as HTMLElement).innerText;
        this.externalSearchParam.emit(this.toSearch);
        this.hoverOver(elements[maxValue] as HTMLElement);
      } else {
        const currentId = current[current.length - 1].id;
        console.log(currentId);
        const elArray = Array.from(elements).map(element => [element.id, element]);
        const elDic = Object.assign({}, ...elArray.map((x) => ({[x[0].toString()]: x[1]})))
        let cNmbr = parseInt(currentId.replace('suggestitem_', ''));
        cNmbr -= 1;
        current.forEach(element => {
          element.removeAttribute("labelSelected");
          this.hoverOut(element as HTMLElement);
        });
        const newId = 'suggestitem_' + cNmbr;
        if (elDic[newId]) {
          const res = elDic[newId] as HTMLElement;
          res.scrollIntoView(false);

          this.toSearch = (res as HTMLElement).innerText;
          this.externalSearchParam.emit(this.toSearch);
          res.setAttribute("labelSelected", "true");
          this.hoverOver(res);
        }
      }
    }
  }

  cursorDown() {
    console.log("Cursor Down")
    const elements = document.querySelectorAll('[id^="suggestitem_"]');
    if (elements && elements.length > 0) {
      const current = Array.from(elements).filter(element => element.attributes.getNamedItem('labelSelected'));
      if (!current || current.length == 0) {
        elements[0].setAttribute("labelSelected", "true");
        elements[0].scrollIntoView(false);
        if (elements[0]) {
          this.toSearch = (elements[0] as HTMLElement).innerText;
          this.externalSearchParam.emit(this.toSearch);
        }
        this.hoverOver(elements[0] as HTMLElement);
      } else {
        const currentId = current[current.length - 1].id;
        console.log(currentId);
        const elArray = Array.from(elements).map(element => [element.id, element]);
        const elDic = Object.assign({}, ...elArray.map((x) => ({[x[0].toString()]: x[1]})))
        let cNmbr = parseInt(currentId.replace('suggestitem_', ''));
        cNmbr += 1;
        current.forEach(element => {
          element.removeAttribute("labelSelected");
          this.hoverOut(element as HTMLElement);
        });
        const newId = 'suggestitem_' + cNmbr;
        if (elDic[newId]) {
          const res = elDic[newId] as HTMLElement;
          res.scrollIntoView(false);

          this.toSearch = (res as HTMLElement).innerText;
          this.externalSearchParam.emit(this.toSearch);
          res.setAttribute("labelSelected", "true");
          this.hoverOver(res);
        }
      }
    }
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

    this.sub = this
      .suggestService
      .getSuggest(request)
      .subscribe(layer => this.responses = layer);
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
