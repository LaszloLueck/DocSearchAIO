import {Component, OnDestroy, OnInit} from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";
import {Subscription} from "rxjs";
import {ActionContentData} from "../config/interfaces/ActionContentData";
import {AlternateReturn} from "../config/interfaces/AlternateReturn";
import {ActionContentDataService} from "./services/action-content-data.service";
import {BaseError} from "../config/interfaces/DocSearchConfiguration";
import {Either, match} from "../../generic/either";
import {NgbdAlertSelfclosing} from "../../main/alerts/alert-selfclosing";

@Component({
  selector: 'app-actions',
  templateUrl: './actions.component.html',
  styleUrls: ['./actions.component.scss']
})
export class ActionsComponent implements OnInit, OnDestroy {

  private subscription!: Subscription;
  public data!: ActionContentData;
  public alternateReturn!: AlternateReturn;
  public message!: string;
  public alertClosed: boolean = true;

  constructor(private commonDataService: CommonDataService, private actionContentDataService: ActionContentDataService) { }

  ngOnInit(): void {
    this.commonDataService.sendData('Aktionen');
    this.loadData();
  }

  resumeTrigger(triggerName: string, groupName: string): void {
    this.message = "KFEKEPRERKF";
    this.alertClosed = false;
    setTimeout(() => this.alertClosed = true, 3000);
  }

  pauseTrigger(triggerName: string, groupName: string): void {

  }

  instantStartJob(jobName: string, groupName: string): void {

  }

  reindexAndStartJob(jobName: string, groupName: string): void {

  }


  loadData(): void {
    this.subscription = this
      .actionContentDataService
      .getActionData()
      .subscribe((data: Either<BaseError, ActionContentData>) => {
        match(
          data,
          left => this.alternateReturn = left,
          right => this.data = right
        )
      });
  }


  ngOnDestroy(): void {
    this
      .subscription
      .unsubscribe();
  }



}
