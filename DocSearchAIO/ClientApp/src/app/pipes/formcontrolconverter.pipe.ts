import {Pipe, PipeTransform} from "@angular/core";
import {AbstractControl, UntypedFormControl} from "@angular/forms";

@Pipe({
  name: 'formControlConverter'
})
export class FormControlConverterPipe implements PipeTransform {
  transform(abstractControl: AbstractControl | null): UntypedFormControl {
    return abstractControl as UntypedFormControl;
  }

}
