import {Injectable} from "@angular/core";
import {LocalStorageDataset} from "../main/interfaces/LocalStorageDataset";
import {Observable, of, throwError} from "rxjs";


@Injectable()
export class LocalStorageService {
  localStorageKey: string = 'localStorageItems';

  getDataAsync(): Observable<LocalStorageDataset>{
    const data = this.getData();
    if(data){
      return of(data)
    }
    return new Observable<LocalStorageDataset>();
  }

  getData(): LocalStorageDataset | undefined {
    const returnItemAsString = localStorage.getItem(this.localStorageKey);
    if (returnItemAsString != null)
      return JSON.parse(returnItemAsString)

    return undefined;
  }

  setData(localStorageDataset: LocalStorageDataset): void {
    const toStore = JSON.stringify(localStorageDataset);
    console.log("TS: " + toStore);
    localStorage.setItem(this.localStorageKey, toStore);
  }

  removeData(): void {
    localStorage.removeItem(this.localStorageKey);
  }

}
