import {Component, Input, OnInit} from '@angular/core';
import {AbstractControl, UntypedFormArray, UntypedFormControl} from "@angular/forms";

@Component({
  selector: 'app-index-configuration',
  templateUrl: './index-configuration.component.html',
  styleUrls: ['./index-configuration.component.scss']
})
export class IndexConfigurationComponent implements OnInit {
  @Input() elasticEndpoints!: UntypedFormArray

  constructor() { }

  ngOnInit(): void {
  }

  removeElasticInstance(index: number): void {
    this.elasticEndpoints.removeAt(index);
  }

  addNewElasticInstance(): void {
    this.elasticEndpoints.push(new UntypedFormControl(''))
  }

  returnFalseIfIndexLimit(indexLength: number, currentIndex: number) {
    return indexLength == 1 && currentIndex == 0
  }

}
