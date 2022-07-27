import {Component, Input, OnInit} from '@angular/core';
import {TriggerElement} from "../../config/interfaces/SchedulerStatisticResponse";

@Component({
  selector: 'app-triggerelement',
  templateUrl: './trigger-element.component.html',
  styleUrls: ['./trigger-element.component.scss']
})
export class TriggerElementComponent implements OnInit {
  @Input() public triggerElement!: TriggerElement;

  constructor() { }

  ngOnInit(): void {
  }

}
