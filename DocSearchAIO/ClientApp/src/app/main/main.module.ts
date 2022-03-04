import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MainRoutingModule } from './main-routing.module';
import { MainComponent } from './main/main.component';
import { StatsCounterComponent } from './stats-counter/stats-counter.component';


@NgModule({
  declarations: [
    MainComponent,
    StatsCounterComponent
  ],
  imports: [
    CommonModule,
    MainRoutingModule
  ]
})
export class MainModule { }
