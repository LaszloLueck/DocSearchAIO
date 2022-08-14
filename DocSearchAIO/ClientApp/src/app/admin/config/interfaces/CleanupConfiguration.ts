export interface CleanupConfiguration {
  forComparer: string;
  forIndexSuffix: string;
  startDelay: number;
  runsEvery: number;
  parallelism: number;
  jobName: string;
  triggerName: string;
}
