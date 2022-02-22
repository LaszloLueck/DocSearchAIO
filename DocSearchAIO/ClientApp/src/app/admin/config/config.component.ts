import {Component, OnDestroy, OnInit} from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";
import {ConfigApiService} from "./config-api.service";
import {EMPTY, Subscription} from "rxjs";
import {AlternateReturn} from "./interfaces/AlternateReturn";
import {FormArray, FormBuilder, FormControl, FormGroup, Validators} from "@angular/forms";
import {Router} from "@angular/router";
import {catchError, take} from "rxjs/operators";
import {DocSearchConfiguration} from "./interfaces/DocSearchConfiguration";
import {ProcessorConfiguration} from "./interfaces/ProcessorConfiguration";
import {CleanupConfiguration} from "./interfaces/CleanupConfiguration";

@Component({
  selector: 'app-config',
  templateUrl: './config.component.html',
  styleUrls: ['./config.component.scss']
})
export class ConfigComponent implements OnInit, OnDestroy {
  private configSubscription!: Subscription;
  alternateReturn: AlternateReturn = new AlternateReturn(false, "");
  form!: FormGroup;
  elasticEndpoints: FormArray;
  proc: Map<string, FormGroup>;
  cleanup: Map<string, FormGroup>;

  constructor(private formBuilder: FormBuilder,
              private commonDataService: CommonDataService,
              private configApiService: ConfigApiService,
              private router: Router) {
    this.elasticEndpoints = new FormArray([]);
    this.proc = new Map;
    this.cleanup = new Map;
  }

  ngOnDestroy(): void {
    this.configSubscription?.unsubscribe();
  }

  saveForm(): void {
    const returnValue: DocSearchConfiguration = this.form.value;
    returnValue.elasticEndpoints = this.elasticEndpoints.value;
    returnValue.processorConfigurations = [];
    returnValue.cleanupConfigurations = [];

    this.proc.forEach((formGroup, key) => {
      const fg: ProcessorConfiguration = formGroup.value;
      returnValue.processorConfigurations.push({item1: key, item2: fg});
    });

    this.cleanup.forEach((formGroup, key) => {
      const fg: CleanupConfiguration = formGroup.value;
      returnValue.cleanupConfigurations.push({item1: key, item2: fg});
    });

    this.configApiService.setConfiguration(returnValue)
      .pipe(
        take(1)
      ).subscribe(ret => {
        if(ret)
          this.doCancel();
    })
  }

  doCancel(): void {
    this.router.navigate(['/home']);
  }

  removeElasticInstance(index: number): void {
    this.elasticEndpoints.removeAt(index);
  }

  addNewElasticInstance(): void {
    this.elasticEndpoints.push(new FormControl(''))
  }

  returnFalseIfIndexLimit(indexLength: number, currentIndex: number) {
    return indexLength == 1 && currentIndex == 0
  }

  ngOnInit(): void {
    this.commonDataService.sendData('Konfiguration');
    this.configSubscription = this
      .configApiService
      .getConfiguration()
      .pipe(
        take(1),
        catchError((err) => {
          console.error(err);
          this.alternateReturn = new AlternateReturn(true, err.message());
          return EMPTY;
        })
      ).subscribe((m: DocSearchConfiguration) => {
        m.elasticEndpoints.forEach(entry => {
          this.elasticEndpoints.push(new FormControl(entry))
        });

        m.processorConfigurations.forEach(tuple => {
          const key = tuple.item1;
          const value = tuple.item2;
          const fg = new FormGroup({});

          fg.addControl('runsEvery', new FormControl(value.runsEvery, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]))
          fg.addControl('startDelay', new FormControl(value.startDelay, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]))
          fg.addControl('parallelism', new FormControl(value.parallelism, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]))
          fg.addControl('fileExtension', new FormControl(value.fileExtension, [Validators.required]))
          fg.addControl('excludeFilter', new FormControl(value.excludeFilter))
          fg.addControl('jobName', new FormControl(value.jobName, [Validators.required]))
          fg.addControl('triggerName', new FormControl(value.triggerName, [Validators.required]))
          fg.addControl('indexSuffix', new FormControl(value.indexSuffix, [Validators.required]))

          this.proc.set(key, fg);
        });

        m.cleanupConfigurations.forEach(tuple => {
          const key = tuple.item1;
          const value = tuple.item2;
          const fg = new FormGroup({});

          fg.addControl('runsEvery', new FormControl(value.runsEvery, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]));
          fg.addControl('startDelay', new FormControl(value.startDelay, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]));
          fg.addControl('parallelism', new FormControl(value.parallelism, [Validators.required, Validators.pattern('/^-?(0|[1-9]\d*)?$/')]));
          fg.addControl('forComparer', new FormControl(value.forComparer, [Validators.required]));
          fg.addControl('jobName', new FormControl(value.jobName, [Validators.required]));
          fg.addControl('triggerName', new FormControl(value.triggerName, [Validators.required]));
          fg.addControl('forIndexSuffix', new FormControl(value.forIndexSuffix, [Validators.required]));
          this.cleanup.set(key, fg);
        });

        this.form = this.formBuilder.group({
          'scanPath': [m.scanPath, [Validators.required]],
          'indexName': [m.indexName, [Validators.required]],
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
