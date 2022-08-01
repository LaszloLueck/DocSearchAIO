import {CommonDataService} from "../../services/CommonDataService";
import {ConfigApiService} from "./config-api.service";
import {Subscription} from "rxjs";
import {AlternateReturn} from "./interfaces/AlternateReturn";
import {UntypedFormArray, UntypedFormBuilder, UntypedFormControl, UntypedFormGroup, Validators} from "@angular/forms";
import {Router} from "@angular/router";
import {take} from "rxjs/operators";
import {BaseError, DocSearchConfiguration} from "./interfaces/DocSearchConfiguration";
import {Component, OnDestroy, OnInit} from "@angular/core";
import {Either, match} from "./Either";

@Component({
  selector: 'app-config',
  templateUrl: './config.component.html',
  styleUrls: ['./config.component.scss']
})
export class ConfigComponent implements OnInit, OnDestroy {
  private configSubscription!: Subscription;
  alternateReturn!: AlternateReturn;
  form!: UntypedFormGroup;
  elasticEndpoints: UntypedFormArray;
  proc: Map<string, UntypedFormGroup>;
  cleanup: Map<string, UntypedFormGroup>;
  externalControlsValid: boolean = true;
  configuration!: DocSearchConfiguration;

  constructor(private formBuilder: UntypedFormBuilder,
              private commonDataService: CommonDataService,
              private configApiService: ConfigApiService,
              private router: Router) {
    this.elasticEndpoints = new UntypedFormArray([]);
    this.proc = new Map;
    this.cleanup = new Map;
  }

  ngOnDestroy(): void {
    this.configSubscription?.unsubscribe();
  }

  checkIfValidEvent(event: boolean): void {
    this.externalControlsValid = event;
  }

  saveForm(): void {
    const returnValue: DocSearchConfiguration = this.form.value;
    returnValue.elasticEndpoints = this.elasticEndpoints.value;
    returnValue.processorConfigurations = {};
    returnValue.cleanupConfigurations = {};

    this.proc.forEach((formGroup, key) => {
      returnValue.processorConfigurations[key] = formGroup.value;
    });

    this.cleanup.forEach((formGroup, key) => {
      returnValue.cleanupConfigurations[key] = formGroup.value;
    });

    this.configApiService.setConfiguration(returnValue)
      .pipe(
        take(1)
      ).subscribe(ret => {
      if (ret)
        this.router.navigate(['/home']);
    })
  }

  ngOnInit(): void {
    this.commonDataService.sendData('Konfiguration');
    this.configSubscription = this
      .configApiService
      .getConfiguration()
      .subscribe((either: Either<BaseError, DocSearchConfiguration>) => {

        match(
          either,
          left => {
            this.alternateReturn = new AlternateReturn(left.errorMessage, left.operation, left.errorCode);
          }, right => {
            this.configuration = right;

            this.configuration.elasticEndpoints.forEach(entry => {
              this.elasticEndpoints.push(new UntypedFormControl(entry))
            });
            for (const key in this.configuration.processorConfigurations) {
              const value = this.configuration.processorConfigurations[key];
              const fg = new UntypedFormGroup({});

              fg.addControl('runsEvery', new UntypedFormControl(value.runsEvery, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]))
              fg.addControl('startDelay', new UntypedFormControl(value.startDelay, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]))
              fg.addControl('parallelism', new UntypedFormControl(value.parallelism, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]))
              fg.addControl('fileExtension', new UntypedFormControl(value.fileExtension, [Validators.required]))
              fg.addControl('excludeFilter', new UntypedFormControl(value.excludeFilter))
              fg.addControl('jobName', new UntypedFormControl(value.jobName, [Validators.required]))
              fg.addControl('triggerName', new UntypedFormControl(value.triggerName, [Validators.required]))
              fg.addControl('indexSuffix', new UntypedFormControl(value.indexSuffix, [Validators.required]))

              this.proc.set(key, fg);
            }

            for (const key in this.configuration.cleanupConfigurations) {
              const value = this.configuration.cleanupConfigurations[key];
              const fg = new UntypedFormGroup({});

              fg.addControl('runsEvery', new UntypedFormControl(value.runsEvery, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]));
              fg.addControl('startDelay', new UntypedFormControl(value.startDelay, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]));
              fg.addControl('parallelism', new UntypedFormControl(value.parallelism, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]));
              fg.addControl('forComparer', new UntypedFormControl(value.forComparer, [Validators.required]));
              fg.addControl('jobName', new UntypedFormControl(value.jobName, [Validators.required]));
              fg.addControl('triggerName', new UntypedFormControl(value.triggerName, [Validators.required]));
              fg.addControl('forIndexSuffix', new UntypedFormControl(value.forIndexSuffix, [Validators.required]));
              this.cleanup.set(key, fg);
            }

            this.form = this.formBuilder.group({
              'scanPath': [this.configuration.scanPath, [Validators.required]],
              'indexName': [this.configuration.indexName, [Validators.required]],
              'elasticUser': [this.configuration.elasticUser, []],
              'elasticPassword': [this.configuration.elasticPassword, []],
              'schedulerName': [this.configuration.schedulerName, [Validators.required]],
              'schedulerId': [this.configuration.schedulerId, [Validators.required]],
              'actorSystemName': [this.configuration.actorSystemName, [Validators.required]],
              'processorGroupName': [this.configuration.processorGroupName, [Validators.required]],
              'cleanupGroupName': [this.configuration.cleanupGroupName, [Validators.required]],
              'uriReplacement': [this.configuration.uriReplacement, []],
              'comparerDirectory': [this.configuration.comparerDirectory, [Validators.required]],
              'statisticsDirectory': [this.configuration.statisticsDirectory, [Validators.required]],
            });
          });
      });
  }

}
