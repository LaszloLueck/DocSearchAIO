import {UntypedFormGroup} from "@angular/forms";
import {Component, Input, OnInit} from "@angular/core";

@Component({
  selector: 'app-static-element',
  templateUrl: './static-element.component.html',
  styleUrls: ['./static-element.component.scss']
})
export class StaticElementComponent implements OnInit {
  @Input() formGroup!: UntypedFormGroup
  @Input() name!: string
  @Input() id!: string
  @Input() type: string = "text"

  constructor() { }

  ngOnInit(): void {
  }

}
