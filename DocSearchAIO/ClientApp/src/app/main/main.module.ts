import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MainRoutingModule } from './main-routing.module';
import { MainComponent } from './main/main.component';
import { StatsCounterComponent } from './stats-counter/stats-counter.component';
import { SearchbarComponent } from './searchbar/searchbar.component';
import { PaginationComponent } from './pagination/pagination.component';
import { ResultpageComponent } from './resultpage/resultpage.component';
import {FormsModule} from "@angular/forms";


@NgModule({
  declarations: [
    MainComponent,
    StatsCounterComponent,
    SearchbarComponent,
    PaginationComponent,
    ResultpageComponent
  ],
  imports: [
    CommonModule,
    MainRoutingModule,
    FormsModule
  ]
})
export class MainModule { }
