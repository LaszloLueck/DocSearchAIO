import {Component, Input, OnInit} from "@angular/core";
import {NgbActiveModal, NgbModal} from "@ng-bootstrap/ng-bootstrap";
import {LocalStorageDataset} from "../interfaces/LocalStorageDataset";
import {LocalStorageService} from "../../services/localStorageService";

export enum DocumentType {
  Word,
  Excel,
  Powerpoint,
  Pdf,
  Eml,
  Msg,
}

@Component({
  selector: 'optionmodal-content',
  templateUrl: './optionmodal-component.template.html'
})
export class OptionModalContent {
  @Input() localStorageDataSet!: LocalStorageDataset;
  docType = DocumentType;

  constructor(public activeModal: NgbActiveModal, private localStorageService: LocalStorageService) {
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

  itemsPerPageCountArray(): number[] {
    return [25, 50, 75, 100]
  }

  itemsClassesPerCondition = (value: number) => {
    return {
      'active': this.localStorageDataSet.itemsPerPage === value
    }
  }

  clickDropDown(value: number): void {
    this.localStorageDataSet.itemsPerPage = value;
    this.clickSaveLocalStorage()
  }

  clickSaveLocalStorage(): void {
    this.localStorageService.setData(this.localStorageDataSet);
  }

}

@Component({
  selector: 'optionmodal-component',
  templateUrl: './optionmodal-component.html'
})
export class OptionModalComponent {
  @Input() localStorageDataSet!: LocalStorageDataset;

  constructor(private modalService: NgbModal) {
  }

  open(): void {
    const modalRef = this.modalService.open(OptionModalContent, {size: 'xl'});
    modalRef.componentInstance.localStorageDataSet = this.localStorageDataSet;

  }

}
