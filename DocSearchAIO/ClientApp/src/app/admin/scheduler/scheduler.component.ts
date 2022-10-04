import {Component, OnDestroy, OnInit} from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";
import {SchedulerDataService} from "./services/scheduler-data.service";
import {Subscription} from "rxjs";
import {SchedulerStatisticResponseBase} from "../config/interfaces/SchedulerStatisticResponse";
import {Either, match} from "../config/Either";
import {BaseError} from "../config/interfaces/DocSearchConfiguration";
import {AlternateReturn} from "../config/interfaces/AlternateReturn";

@Component({
  selector: 'app-scheduler',
  templateUrl: './scheduler.component.html',
  styleUrls: ['./scheduler.component.scss']
})
export class SchedulerComponent implements OnInit, OnDestroy {
  private subscription!: Subscription;
  public data!: SchedulerStatisticResponseBase;
  public alternateReturn!: AlternateReturn;

  constructor(private commonDataService: CommonDataService, private schedulerService: SchedulerDataService) {
  }

  ngOnDestroy(): void {
    this
      .subscription
      .unsubscribe();
  }

  ngOnInit(): void {
    this.commonDataService.sendData('Scheduler');
    this.loadData();
  }

  loadData(): void {
    this.subscription = this
      .schedulerService
      .getSchedulerInfo()
      .subscribe((data: Either<BaseError, SchedulerStatisticResponseBase>) => {
        match(
          data,
          left => {
            this.alternateReturn = left;
          },
          right => {
            this.data = right;
          }
        )
      });
  }


}
