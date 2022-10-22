import {Component, OnDestroy, OnInit} from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";
import {Subscription} from "rxjs";
import {ActionContentData} from "../config/interfaces/ActionContentData";
import {AlternateReturn} from "../config/interfaces/AlternateReturn";
import {ActionContentDataService} from "./services/action-content-data.service";
import {BaseError} from "../config/interfaces/DocSearchConfiguration";
import {Either, match} from "../../generic/either";
import {NgbdAlertSelfclosing} from "../../main/alerts/alert-selfclosing";
import {InstantStartJobService} from "./services/instant-start-job.service";
import {ReindexAndStartJobService} from "./services/reindex-and-start-job.service";
import {PauseTriggerService} from "./services/pause-trigger.service";
import {ResumeTriggerService} from "./services/resume-trigger.service";
import {TriggerResult} from "../config/interfaces/TriggerResult";
import {TriggerRequest} from "../config/interfaces/TriggerRequest";
import {JobRequest} from "../config/interfaces/JobRequest";
import {JobResult} from "../config/interfaces/JobResult";

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

  constructor(
    private commonDataService: CommonDataService,
    private actionContentDataService: ActionContentDataService,
    private instantStartJobService: InstantStartJobService,
    private reindexAndStartService: ReindexAndStartJobService,
    private pauseTriggerService: PauseTriggerService,
    private resumeTriggerService: ResumeTriggerService
  ) {
  }

  ngOnInit(): void {
    this.commonDataService.sendData('Aktionen');
    this.loadData();
  }

  writeAlert(message: string, timeout: number = 3000): void {
    this.message = message;
    this.alertClosed = false;
    setTimeout(() => this.alertClosed = true, 3000);
  }

  generateErrorString(baseError: BaseError, errorMessage: string): string {
    return errorMessage + " :: FROMRESULT: " + baseError.errorMessage + " :: FROMOPERATION: " + baseError.operation;
  }

  resumeTrigger(triggerName: string, groupName: string): void {
    const triggerData: TriggerRequest = {
      triggerId: triggerName,
      groupId: groupName
    };

    this
      .resumeTriggerService
      .resumeTrigger(triggerData)
      .subscribe((either: Either<BaseError, TriggerResult>) => {
        match(either,
          left => {
            this.writeAlert(this.generateErrorString(left, "An error while resume trigger <" + triggerName + "> occured"))
          },
          right => {
            if(!right.result)
              this.writeAlert("Something went wrong if resuming a trigger " + triggerName);
            this.loadData();
          });
      });
  }

  pauseTrigger(triggerName: string, groupName: string): void {
    const triggerData: TriggerRequest = {
      triggerId: triggerName,
      groupId: groupName
    };

    this
      .pauseTriggerService
      .pauseTrigger(triggerData)
      .subscribe((either: Either<BaseError, TriggerResult>) => {
        match(either,
          left => {
            this.writeAlert(this.generateErrorString(left, "An error while pause trigger <" + triggerName + "> occured"))
          },
          right => {
            if(!right.result)
              this.writeAlert("Something went wrong if pause a trigger " + triggerName);
            this.loadData();
          });
      });
  }

  instantStartJob(jobName: string, groupName: string): void {
    const jobData: JobRequest = {
      jobName: jobName,
      groupId: groupName
    };

    this
      .instantStartJobService
      .instantStartJob(jobData)
      .subscribe((either: Either<BaseError, JobResult>) => {
        match(either,
          left => {
            this.writeAlert(this.generateErrorString(left, "An error while instant start a job <" + jobName + "> occured"))
          },
          right => {
            if(!right.result)
              this.writeAlert("Something went wrong if instant start a job " + jobName);

            this.loadData()
          });
      });

  }

  reindexAndStartJob(jobName: string, groupName: string): void {
    const jobData: JobRequest = {
      jobName: jobName,
      groupId: groupName
    };

    this
      .reindexAndStartService
      .reindexAndStartJob(jobData)
      .subscribe((either: Either<BaseError, JobResult>) => {
        match(either,
          left => {
            this.writeAlert(this.generateErrorString(left, "An error while reindexing and start a job <" + jobName + "> occured"))
          },
          right => {
            if(!right.result)
              this.writeAlert("Something went wrong if reindex and start a job " + jobName);

            this.loadData()
          });
      });
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
