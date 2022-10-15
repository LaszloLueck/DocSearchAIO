
export interface ActionContentData {
  doSearch_Processing: Scheduler;
  doSearch_Cleanup: Scheduler;


}

export interface Scheduler {
  schedulerName: string;
  triggers: Trigger[];
}


export interface Trigger {
  triggerName: string;
  groupName: string;
  currentState: string;
  jobName: string;
  jobState: number;
}
