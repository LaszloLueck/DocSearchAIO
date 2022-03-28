import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MainRoutingModule } from './main-routing.module';
import { MainComponent } from './main/main.component';
import { ResultpageComponent } from './resultpage/resultpage.component';
import {FormsModule} from "@angular/forms";
import {NgbAlertModule, NgbModule, NgbPaginationModule, NgbPopoverModule} from "@ng-bootstrap/ng-bootstrap";
import {RisprepositorylinkComponent, RisprepositorylinkContent} from "./modals/risprepositorylink-component";
import {DownloadLinkComponent, DownloadlinkContent} from "./modals/downloadlink-component";


@NgModule({
  declarations: [
    MainComponent,
    ResultpageComponent,
    RisprepositorylinkComponent,
    RisprepositorylinkContent,
    DownloadLinkComponent,
    DownloadlinkContent
  ],
  imports: [
    CommonModule,
    MainRoutingModule,
    FormsModule,
    NgbAlertModule,
    NgbPaginationModule,
    NgbPopoverModule,
    NgbModule
  ],
  exports: [
    RisprepositorylinkComponent,
    DownloadLinkComponent
  ],
  bootstrap: [
    RisprepositorylinkComponent,
    DownloadLinkComponent
  ]
})
export class MainModule { }
