import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {LocalStorageDataset} from "../../interfaces/LocalStorageDataset";
import {LocalStorageService} from "../../../services/localStorageService";
import {DocumentType} from "../../enums/document-type";

@Component({
  selector: 'app-offcanvas',
  templateUrl: './offcanvas.component.html',
  styleUrls: ['./offcanvas.component.scss']
})
export class OffcanvasComponent implements OnInit {
  isOpen: boolean = false;
  @Input() localStorageDataSet!: LocalStorageDataset;
  @Output() doSearchEvent: EventEmitter<any> = new EventEmitter<any>();
  docType: typeof DocumentType;

  constructor(private localStorageService: LocalStorageService) {
    this.docType = DocumentType;
  }

  doSearch(): void {
    this.doSearchEvent.emit();
  }

  ngOnInit(): void {
  }

  itemsPerPageCountArray(): number[] {
    return [25, 50, 75, 100]
  }

  itemsStylePerCondition = (value: number) => this.localStorageDataSet.itemsPerPage === value ? {'background-color': '#6c757d'} : {}

  itemsClassesPerCondition = (value: number) => {
    return {
      'active': this.localStorageDataSet.itemsPerPage === value,
      'btn': true,
      'btn-sm': true,
      'btn-secondary': true
    }
  }

  disableItemWhileCondition(docType: DocumentType): boolean {
    switch (docType) {
      case DocumentType.Word:
        return !this.localStorageDataSet.wordFilterActive || (this.localStorageDataSet.filterWord && !this.localStorageDataSet.filterExcel && !this.localStorageDataSet.filterMsg && !this.localStorageDataSet.filterEml && !this.localStorageDataSet.filterPdf && !this.localStorageDataSet.filterPowerpoint)
      case DocumentType.Excel:
        return !this.localStorageDataSet.excelFilterActive || (!this.localStorageDataSet.filterWord && this.localStorageDataSet.filterExcel && !this.localStorageDataSet.filterMsg && !this.localStorageDataSet.filterEml && !this.localStorageDataSet.filterPdf && !this.localStorageDataSet.filterPowerpoint)
      case DocumentType.Powerpoint:
        return !this.localStorageDataSet.powerpointFilterActive || (!this.localStorageDataSet.filterWord && !this.localStorageDataSet.filterExcel && !this.localStorageDataSet.filterMsg && !this.localStorageDataSet.filterEml && !this.localStorageDataSet.filterPdf && this.localStorageDataSet.filterPowerpoint)
      case DocumentType.Pdf:
        return !this.localStorageDataSet.pdfFilterActive || (!this.localStorageDataSet.filterWord && !this.localStorageDataSet.filterExcel && !this.localStorageDataSet.filterMsg && !this.localStorageDataSet.filterEml && this.localStorageDataSet.filterPdf && !this.localStorageDataSet.filterPowerpoint)
      case DocumentType.Eml:
        return !this.localStorageDataSet.emlFilterActive || (!this.localStorageDataSet.filterWord && !this.localStorageDataSet.filterExcel && !this.localStorageDataSet.filterMsg && this.localStorageDataSet.filterEml && !this.localStorageDataSet.filterPdf && !this.localStorageDataSet.filterPowerpoint)
      case DocumentType.Msg:
        return !this.localStorageDataSet.msgFilterActive || (!this.localStorageDataSet.filterWord && !this.localStorageDataSet.filterExcel && this.localStorageDataSet.filterMsg && !this.localStorageDataSet.filterEml && !this.localStorageDataSet.filterPdf && !this.localStorageDataSet.filterPowerpoint)
      default:
        return false
    }
  }

  clickDropDown(value: number): void {
    this.localStorageDataSet.itemsPerPage = value;
    this.clickSaveLocalStorage()
  }

  clickSaveLocalStorage(): void {
    this.localStorageService.setData(this.localStorageDataSet);
  }

  openNav(): void {
    if (!this.isOpen) {
      if (document.getElementById('mySidenav')) {
        document.getElementById("mySidenav")!.classList.add("sidenav-option-in");
        document.getElementById("mySidenav")!.classList.remove("sidenav-option-out")
      }

      if (document.getElementById("fader"))
        document.getElementById("fader")!.className = "fade-in";

      if (document.getElementById("main"))
        document.getElementById("main")!.style.marginLeft = "15%";
    }

    this.isOpen = true;

  }

  closeNav(): void {
    if (document.getElementById("mySidenav")) {
      document.getElementById("mySidenav")!.classList.add("sidenav-option-out");
      document.getElementById("mySidenav")!.classList.remove("sidenav-option-in");
    }
    if (document.getElementById("fader"))
      document.getElementById("fader")!.className = "fade-out";

    if (document.getElementById("main"))
      document.getElementById("main")!.style.marginLeft = "0";

    this.isOpen = false;
  }

}
