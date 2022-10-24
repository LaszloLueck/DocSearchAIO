import {ProcessorConfiguration} from "./ProcessorConfiguration";

export interface IndexStatisticsResponse {
  entireDocCount: number;
  entireSizeInBytes: number;
  indexStatisticModels: IndexStatistic[];
  runtimeStatistics : { [item1: string] : RunnableStatistic };
}

export interface IndexStatistic {
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
  indexedDocCount: number;
  processingError: number;
  startJob: Date;
  endJob: Date;
  elapsedTimeMillis: number;
  cacheEntry: CacheEntry
}

export interface CacheEntry {
  cacheKey: string;
  dateTime: Date;
  jobState: number;
}
