import {NgModule} from '@angular/core';
import {CommonModule} from '@angular/common';

import {AdminRoutingModule} from './admin-routing.module';
import {ConfigComponent} from './config/config.component';
import { SchedulerComponent } from './scheduler/scheduler.component';
import { StatisticsComponent } from './statistics/statistics.component';
import { ActionsComponent } from './actions/actions.component';
import {ReactiveFormsModule} from "@angular/forms";
import {FormControlConverterPipe} from "../pipes/formcontrolconverter.pipe";
import {FormControlValidatorPipe} from "../pipes/fomcontrolvalidator.pipe";
import { StaticElementComponent } from './config/static-element/static-element.component';
import { DynamicElementComponent } from './config/dynamic-element/dynamic-element.component';
import { IndexConfigurationComponent } from './config/index-configuration/index-configuration.component';
import { ButtonBarComponent } from './config/button-bar/button-bar.component';
import { SchedulerstatisticComponent } from './scheduler/schedulerstatistic/schedulerstatistic.component';
import { TriggerElementComponent } from './scheduler/triggerelement/trigger-element.component';
import { DetailComponent } from './scheduler/component/detail/detail.component';
import { BytesvisualizerPipe } from './statistics/pipes/bytesvisualizer.pipe';


@NgModule({
  declarations: [
    ConfigComponent,
    SchedulerComponent,
    StatisticsComponent,
    ActionsComponent,
    FormControlConverterPipe,
    FormControlValidatorPipe,
    StaticElementComponent,
    DynamicElementComponent,
    IndexConfigurationComponent,
    ButtonBarComponent,
    SchedulerstatisticComponent,
    TriggerElementComponent,
    DetailComponent,
    BytesvisualizerPipe
  ],
    imports: [
        CommonModule,
        AdminRoutingModule,
        ReactiveFormsModule
    ],
  providers: [
    FormControlValidatorPipe
  ]
})
export class AdminModule {
}
