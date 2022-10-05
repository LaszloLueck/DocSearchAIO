import {ComponentFixture, TestBed} from "@angular/core/testing";
import {ConfigComponent} from "./config.component";
import {ReactiveFormsModule} from "@angular/forms";
import {CommonDataService} from "../../services/CommonDataService";
import {RouterTestingModule} from "@angular/router/testing";
import {of} from "rxjs";
import {BaseError, DocSearchConfiguration} from "./interfaces/DocSearchConfiguration";
import {Either, makeRight} from "../../generic/either";
import {FormControlConverterPipe} from "../../pipes/formcontrolconverter.pipe";
import {DynamicElementComponent} from "./dynamic-element/dynamic-element.component";
import {FormControlValidatorPipe} from "../../pipes/fomcontrolvalidator.pipe";
import {IndexConfigurationComponent} from "./index-configuration/index-configuration.component";
import {ButtonBarComponent} from "./button-bar/button-bar.component";
import {ConfigApiService} from "./services/config-api.service";

describe('ConfigComponent right result', () => {
  let component: ConfigComponent;
  let fixture: ComponentFixture<ConfigComponent>;
  let fakeConfigApiService: ConfigApiService;

  beforeEach(async () => {
    const result: DocSearchConfiguration = {
      scanPath: 'scanpath',
      elasticEndpoints: ['endpoint:1', 'endpoint:2'],
      elasticUser: 'elasticuser',
      elasticPassword: 'elasticpassword',
      indexName: 'indexname',
      schedulerName: 'schedulername',
      schedulerId: 'schedulerid',
      actorSystemName: 'actorsystemname',
      processorGroupName: 'processorgroupname',
      cleanupGroupName: 'cleanupgroupname',
      uriReplacement: 'urireplacement',
      comparerDirectory: 'comparerdirectory',
      statisticsDirectory: 'statisticsdirectory',
      processorConfigurations: {
        ['processConfig']: {
          parallelism: 5,
          startDelay: 100,
          runsEvery: 10,
          excludeFilter: 'excludefilter',
          fileExtension: 'fileextension',
          indexSuffix: 'processsuffix',
          jobName: 'processjobname',
          triggerName: 'processtriggername'
        }
      },
      cleanupConfigurations: {
        ['cleanupConfig']: {
          parallelism: 5,
          startDelay: 100,
          runsEvery: 10,
          forComparer: 'forCleanupComparer',
          forIndexSuffix: 'forindexsuffix',
          jobName: 'cleanupjobname',
          triggerName: 'cleanuptriggername'
        }
      }
    }
    const eitherRight: Either<BaseError, DocSearchConfiguration> = makeRight(result);


    fakeConfigApiService = jasmine.createSpyObj<ConfigApiService>('ConfigApiService', {
        getConfiguration: of(eitherRight),
        setConfiguration: undefined,
      }
    )

    await TestBed.configureTestingModule(
      {
        declarations: [
          FormControlConverterPipe,
          FormControlValidatorPipe,
          ConfigComponent,
          ButtonBarComponent,
          IndexConfigurationComponent,
          DynamicElementComponent
        ],
        imports: [ReactiveFormsModule, RouterTestingModule],
        providers: [
          CommonDataService,
          {provide: ConfigApiService, useValue: fakeConfigApiService},
          FormControlValidatorPipe,
          FormControlConverterPipe
        ]
      }
    ).compileComponents()

    fixture = TestBed.createComponent(ConfigComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should called getConfiguration', () => {
    expect(fakeConfigApiService.getConfiguration).toHaveBeenCalled();
  });

  it('should filled up the result object', () => {
    expect(component.configuration).toBeTruthy();
  });

  it('should not filled the specific return object in case of an error', () => {
    expect(component.alternateReturn).not.toBeTruthy();
  });

  it('should exist a full filled elastic endpoint array', () => {
    expect(component.elasticEndpoints).toBeTruthy();
    expect(component.elasticEndpoints.length).toBe(2);
    expect(component.elasticEndpoints.controls[0].value).toBe('endpoint:1');
  });
});
