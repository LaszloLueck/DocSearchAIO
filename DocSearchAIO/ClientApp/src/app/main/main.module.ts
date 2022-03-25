import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MainRoutingModule } from './main-routing.module';
import { MainComponent } from './main/main.component';
import { ResultpageComponent } from './resultpage/resultpage.component';
import {FormsModule} from "@angular/forms";
import {NgbAlertModule} from "@ng-bootstrap/ng-bootstrap";


@NgModule({
  declarations: [
    MainComponent,
    ResultpageComponent
  ],
  imports: [
    CommonModule,
    MainRoutingModule,
    FormsModule,
    NgbAlertModule
  ]
})
export class MainModule { }
