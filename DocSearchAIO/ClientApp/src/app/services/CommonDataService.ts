import {Injectable} from "@angular/core";
import {Observable, Subject} from "rxjs";


@Injectable()
export class CommonDataService {
  private subject = new Subject<any>();

  sendData(message: string) {
    this.subject.next(message);
  }

  clearData() {
    this.subject.next(null);
  }

  getData(): Observable<any> {
    return this.subject.asObservable();
  }

}
