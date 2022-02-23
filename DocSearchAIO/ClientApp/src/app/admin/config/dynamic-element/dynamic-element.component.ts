import {Component, Input, OnInit} from '@angular/core';
import {FormGroup} from "@angular/forms";

@Component({
  selector: 'app-dynamic-element',
  templateUrl: './dynamic-element.component.html',
  styleUrls: ['./dynamic-element.component.scss']
})
export class DynamicElementComponent implements OnInit {

  @Input() formGroup!: FormGroup
  @Input() internalId!: string
  @Input() namePrefix!: string;
  @Input() nameSuffix!: string;
  @Input() fieldType!: string;
  @Input() validateField!: boolean;

  constructor() { }

  ngOnInit(): void {
  }

}
