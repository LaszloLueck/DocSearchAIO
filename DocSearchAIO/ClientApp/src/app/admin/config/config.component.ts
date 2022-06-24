import {CommonDataService} from "../../services/CommonDataService";
import {ConfigApiService} from "./config-api.service";
import {EMPTY, Subscription, tap} from "rxjs";
import {AlternateReturn} from "./interfaces/AlternateReturn";
import {UntypedFormArray, UntypedFormBuilder, UntypedFormControl, UntypedFormGroup, Validators} from "@angular/forms";
import {Router} from "@angular/router";
import {catchError, take} from "rxjs/operators";
import {DocSearchConfiguration} from "./interfaces/DocSearchConfiguration";
import {ProcessorConfiguration} from "./interfaces/ProcessorConfiguration";
import {Component, OnDestroy, OnInit} from "@angular/core";

@Component({
  selector: 'app-config',
  templateUrl: './config.component.html',
  styleUrls: ['./config.component.scss']
})
export class ConfigComponent implements OnInit, OnDestroy {
  private configSubscription!: Subscription;
  alternateReturn: AlternateReturn = new AlternateReturn(false, "");
  form!: UntypedFormGroup;
  elasticEndpoints: UntypedFormArray;
  proc: Map<string, UntypedFormGroup>;
  cleanup: Map<string, UntypedFormGroup>;
  externalControlsValid: boolean = true;

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

  checkIfValidEvent(event: boolean): void{
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
        if(ret)
          this.router.navigate(['/home']);
    })
  }

  ngOnInit(): void {
    this.commonDataService.sendData('Konfiguration');
    this.configSubscription = this
      .configApiService
      .getConfiguration()
      .pipe(
        take(1),
        tap(_ => console.log('fetching configuration')),
        catchError((err) => {
          console.error(err);
          this.alternateReturn = new AlternateReturn(true, err.message());
          return EMPTY;
        })
      ).subscribe((m: DocSearchConfiguration) => {
        m.elasticEndpoints.forEach(entry => {
          this.elasticEndpoints.push(new UntypedFormControl(entry))
        });

        for(const key in m.processorConfigurations){
          const value = m.processorConfigurations[key];
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

        for(const key in m.cleanupConfigurations){
          const value = m.cleanupConfigurations[key];
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
          'scanPath': [m.scanPath, [Validators.required]],
          'indexName': [m.indexName, [Validators.required]],
          'elasticUser': [m.elasticUser, []],
          'elasticPassword': [m.elasticPassword, []],
          'schedulerName': [m.schedulerName, [Validators.required]],
          'schedulerId': [m.schedulerId, [Validators.required]],
          'actorSystemName': [m.actorSystemName, [Validators.required]],
          'processorGroupName': [m.processorGroupName, [Validators.required]],
          'cleanupGroupName': [m.cleanupGroupName, [Validators.required]],
          'uriReplacement': [m.uriReplacement, []],
          'comparerDirectory': [m.comparerDirectory, [Validators.required]],
          'statisticsDirectory': [m.statisticsDirectory, [Validators.required]],
        });
      });
  }

}
