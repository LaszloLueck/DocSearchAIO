import {Component, Input, OnInit} from '@angular/core';
import {SchedulerStatisticResponse} from "../../config/interfaces/SchedulerStatisticResponse";

@Component({
  selector: 'app-schedulerstatistic',
  templateUrl: './schedulerstatistic.component.html',
  styleUrls: ['./schedulerstatistic.component.scss']
})
export class SchedulerstatisticComponent implements OnInit {
  @Input() public response!: SchedulerStatisticResponse;

  constructor() { }

  renderBadgeState(state: string): string{
    switch (state.toLowerCase()){
      case "gestartet":
        return "badge bg-primary";
    }

    return "badge bg-secondary";
  }

  ngOnInit(): void {
  }

}
