import {CleanupConfiguration} from "./CleanupConfiguration";
import {ProcessorConfiguration} from "./ProcessorConfiguration";

export interface BaseError {
  errorMessage: string;
  errorCode: number;
  operation: string;
}

export interface DocSearchConfiguration {
  scanPath: string;
  elasticEndpoints: string[];
  elasticUser: string;
  elasticPassword: string;
  indexName: string;
  schedulerName: string;
  schedulerId: string;
  actorSystemName: string;
  processorGroupName: string;
  cleanupGroupName: string;
  uriReplacement: string;
  comparerDirectory: string;
  statisticsDirectory: string;
  processorConfigurations: { [item1: string] : ProcessorConfiguration };
  cleanupConfigurations: { [item1: string] : CleanupConfiguration };
}
