import {Injectable} from "@angular/core";
import {LocalStorageDataset} from "../main/interfaces/LocalStorageDataset";
import {BehaviorSubject, Observable} from "rxjs";
import {LocalStorageDefaultDataset} from "../main/interfaces/LocalStorageDefaultDataset";


@Injectable()
export class LocalStorageService {
  localStorageKey: string = 'localStorageItems';

  private _localStorage: BehaviorSubject<LocalStorageDataset>;
  private readonly _localStorageStore: Observable<LocalStorageDataset>;

  constructor() {
    this._localStorage = new BehaviorSubject<LocalStorageDataset>(new LocalStorageDefaultDataset());
    const origin = this.getData();
    this._localStorageStore = this._localStorage.asObservable();
    if (origin)
      this.setData(origin);
  }


  getDataAsync(): Observable<LocalStorageDataset> {
    return this._localStorageStore;
  }

  private getData(): LocalStorageDataset | undefined {
    const returnItemAsString = localStorage.getItem(this.localStorageKey);
    console.log("RC: " + returnItemAsString);
    if (returnItemAsString != null)
      return JSON.parse(returnItemAsString)
    return undefined;
  }

  setData(localStorageDataset: LocalStorageDataset): void {
    const toStore = JSON.stringify(localStorageDataset);
    console.log("TS: " + toStore);
    localStorage.setItem(this.localStorageKey, toStore);
    this._localStorage.next(localStorageDataset);
  }

  removeData(): void {
    localStorage.removeItem(this.localStorageKey);
  }

}
