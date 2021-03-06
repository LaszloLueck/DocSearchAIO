import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MainRoutingModule } from './main-routing.module';
import { MainComponent } from './main/main.component';
import { ResultpageComponent } from './resultpage/resultpage.component';
import {FormsModule} from "@angular/forms";
import {
  NgbAlertModule, NgbDropdownModule,
  NgbModule,
  NgbPaginationModule,
  NgbPopoverModule
} from "@ng-bootstrap/ng-bootstrap";
import {RisprepositorylinkComponent, RisprepositorylinkContent} from "./modals/risprepositorylink-component";
import {DownloadLinkComponent, DownloadlinkContent} from "./modals/downloadlink-component";
import {DocumentInformationComponent, DocumentInformationContent} from "./modals/documentInformation-component";
import { NgbdAlertSelfclosing} from "./alerts/alert-selfclosing";
import { OffcanvasComponent } from './components/offcanvas/offcanvas.component';
import { SuggestionComponent } from './components/suggestion/suggestion.component';
import {MatAutocompleteModule} from "@angular/material/autocomplete";
import {SuggestOrderPipe} from "./pipes/SuggestOrderPipe";
import {LocalStorageService} from "../services/LocalStorageService";

@NgModule({
  declarations: [
    MainComponent,
    ResultpageComponent,
    RisprepositorylinkComponent,
    RisprepositorylinkContent,
    DownloadLinkComponent,
    DownloadlinkContent,
    DocumentInformationComponent,
    DocumentInformationContent,
    NgbdAlertSelfclosing,
    OffcanvasComponent,
    SuggestionComponent,
    SuggestOrderPipe
  ],
    imports: [
        CommonModule,
        MainRoutingModule,
        FormsModule,
        NgbAlertModule,
        NgbPaginationModule,
        NgbPopoverModule,
        NgbDropdownModule,
        NgbModule,
        MatAutocompleteModule
    ],
  exports: [
    RisprepositorylinkComponent,
    DownloadLinkComponent,
    DocumentInformationComponent,
    NgbdAlertSelfclosing
  ],
  bootstrap: [
    RisprepositorylinkComponent,
    DownloadLinkComponent,
    DocumentInformationComponent,
    NgbdAlertSelfclosing
  ],
  providers: [
    LocalStorageService
  ]
})
export class MainModule { }
