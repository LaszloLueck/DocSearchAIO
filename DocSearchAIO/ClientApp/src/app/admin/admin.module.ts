import {NgModule} from '@angular/core';
import {CommonModule} from '@angular/common';

import {AdminRoutingModule} from './admin-routing.module';
import {ConfigComponent} from './config/config.component';
import { SchedulerComponent } from './scheduler/scheduler.component';
import { StatisticsComponent } from './statistics/statistics.component';
import { ActionsComponent } from './actions/actions.component';
import {ReactiveFormsModule} from "@angular/forms";
import {FormControlConverterPipe} from "../pipes/formcontrolconverter.pipe";


@NgModule({
  declarations: [
    ConfigComponent,
    SchedulerComponent,
    StatisticsComponent,
    ActionsComponent,
    FormControlConverterPipe
  ],
    imports: [
        CommonModule,
        AdminRoutingModule,
        ReactiveFormsModule
    ]
})
export class AdminModule {
}
