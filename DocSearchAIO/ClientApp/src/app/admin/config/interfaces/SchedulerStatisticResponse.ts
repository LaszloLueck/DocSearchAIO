export interface SchedulerStatisticResponseBase {
  docSearch_Processing: SchedulerStatisticResponse,
  docSearch_Cleanup: SchedulerStatisticResponse
}

export interface SchedulerStatisticResponse {
  schedulerName: string,
  schedulerInstanceId: string,
  state: string,
  triggerElements: TriggerElement[]
}

export interface TriggerElement {
  triggerName: string,
  groupName: string,
  nextFireTime: Date,
  startTime: Date,
  lastFireTime: Date,
  triggerState: string,
  description: string,
  processingState: boolean,
  jobName: string
}
