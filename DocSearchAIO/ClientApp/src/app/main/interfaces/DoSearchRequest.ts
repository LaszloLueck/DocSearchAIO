
export interface DoSearchRequest {
  searchPhrase: string;
  from: number;
  size: number;
  filterWord: boolean;
  filterExcel: boolean;
  filterPowerpoint: boolean;
  filterPdf: boolean;
  filterMsg: boolean;
  filterEml: boolean;
}
