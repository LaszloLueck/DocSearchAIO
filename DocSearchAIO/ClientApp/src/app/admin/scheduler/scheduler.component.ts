import {Component, OnDestroy, OnInit} from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";
import {SchedulerserviceService} from "./services/schedulerservice.service";
import {Observable, Subscription} from "rxjs";
import {SchedulerStatisticResponseBase} from "../config/interfaces/SchedulerStatisticResponse";

@Component({
  selector: 'app-scheduler',
  templateUrl: './scheduler.component.html',
  styleUrls: ['./scheduler.component.scss']
})
export class SchedulerComponent implements OnInit, OnDestroy {
  private subscription!: Subscription;
  public data!: SchedulerStatisticResponseBase;

  constructor(private commonDataService: CommonDataService, private schedulerService: SchedulerserviceService) { }

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
      .subscribe(data => this.data = data);
  }



}
