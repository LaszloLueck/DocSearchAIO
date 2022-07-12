import {Pipe, PipeTransform} from "@angular/core";
import {SuggestResponseDetail} from "../interfaces/SuggestResponseDetail";
import {LocalStorageDataset} from "../interfaces/LocalStorageDataset";


@Pipe({
  name: 'suggestOrder'
})
export class SuggestOrderPipe implements PipeTransform {
  transform(suggests: SuggestResponseDetail[], searchTerm: string, localStorageDataset: LocalStorageDataset): SuggestResponseDetail[] {
    if(suggests.length > 1 && suggests.map(s => s.label).includes(searchTerm) && suggests.findIndex(d => d.label === searchTerm) > 0){
      const elementPlace = suggests.findIndex(d => d.label === searchTerm);
      const element = suggests[elementPlace];
      const remaining = suggests.filter(d => d.label != searchTerm);
      remaining.unshift(element);
      return remaining;
    }

    return suggests;

  }
}

