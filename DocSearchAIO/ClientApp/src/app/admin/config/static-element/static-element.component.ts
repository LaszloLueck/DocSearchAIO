import {Component, Input, OnInit} from '@angular/core';
import {FormGroup} from "@angular/forms";

@Component({
  selector: 'app-static-element',
  templateUrl: './static-element.component.html',
  styleUrls: ['./static-element.component.scss']
})
export class StaticElementComponent implements OnInit {
  @Input() formGroup!: FormGroup
  @Input() name!: string
  @Input() id!: string

  constructor() { }

  ngOnInit(): void {
  }

}