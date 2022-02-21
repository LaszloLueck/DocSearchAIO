import {Pipe, PipeTransform} from "@angular/core";
import {AbstractControl, FormControl} from "@angular/forms";

@Pipe({
  name: 'formControlConverter'
})
export class FormControlConverterPipe implements PipeTransform {
  transform(abstractControl: AbstractControl | null): FormControl {
    return abstractControl as FormControl;
  }

}
