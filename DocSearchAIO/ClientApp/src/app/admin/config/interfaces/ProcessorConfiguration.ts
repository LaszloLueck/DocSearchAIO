export interface ProcessorConfiguration {
  parallelism: number;
  startDelay: number;
  runsEvery: number;
  excludeFilter: string;
  indexSuffix: string;
  fileExtension: string;
  jobName: string;
  triggerName: string;
}
