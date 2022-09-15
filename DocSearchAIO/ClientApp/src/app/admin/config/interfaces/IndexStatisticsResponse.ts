
export interface IndexStatisticsResponse {
  entireDocCount: number;
  entireSizeInBytes: number;
  runtimeStatistics: { [item1: string] : RunnableStatistic };
  indexStatisticModels : IndesStatistic[];
}

export interface IndesStatistic {
  indexName: string;
  docCount: number;
  sizeInBytes: number;
  fetchTimeMs: number;
  fetchTotal: number;
  queryTimeMs: number;
  queryTotal: number;
  suggestTimeMs: number;
  suggestTotal: number;
}

export interface RunnableStatistic {
  id: string;
  entireDocCount: number;
  indexDocCount: number;
  processingError: number;
  startJob: Date;
  endJob: Date;
  elapsedTimeMillis: number;
}
