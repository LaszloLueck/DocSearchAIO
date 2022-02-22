import {Pipe, PipeTransform} from "@angular/core";
import {AbstractControl} from "@angular/forms";

@Pipe({
  name: 'formControlValidator',
  pure: false
})
export class FormControlValidatorPipe implements PipeTransform {
  transform(control: AbstractControl | null): boolean {
    return (control == null) ? true : control.invalid && (control.dirty || control.touched)
  }
}
