import {LocalStorageDataset} from "./LocalStorageDataset";

export class LocalStorageDefaultDataset implements LocalStorageDataset {
  filterExcel: boolean = true;
  filterWord: boolean = true;
  filterEml: boolean = true;
  filterPdf: boolean = true;
  filterMsg: boolean = true;
  filterPowerpoint: boolean = true;
  itemsPerPage: number = 50;
  emlFilterActive: boolean = false;
  excelFilterActive: boolean = false;
  msgFilterActive: boolean = false;
  pdfFilterActive: boolean = false;
  powerpointFilterActive: boolean = false;
  wordFilterActive: boolean = false;

}
