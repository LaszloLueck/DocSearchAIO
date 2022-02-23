import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {FormControl, FormGroup} from "@angular/forms";
import {FormControlValidatorPipe} from "../../../pipes/fomcontrolvalidator.pipe";

@Component({
  selector: 'app-dynamic-element',
  templateUrl: './dynamic-element.component.html',
  styleUrls: ['./dynamic-element.component.scss']
})
export class DynamicElementComponent implements OnInit {

  @Input() formControlInternal!: FormControl;
  @Input() namePrefix!: string;
  @Input() nameSuffix!: string;
  @Input() fieldType!: string;
  @Input() validateField!: boolean;
  @Output() fieldIsValid: EventEmitter<boolean> = new EventEmitter<boolean>();

  constructor(private validatorPipe: FormControlValidatorPipe) { }

  checkFieldIsValid(): void {
    if(this.formControlInternal.value.length == 0){
      this.fieldIsValid.emit(false);
    } else {
      this.fieldIsValid.emit(this.validatorPipe.transform(this.formControlInternal));
    }
  }


  ngOnInit(): void {

  }

}
