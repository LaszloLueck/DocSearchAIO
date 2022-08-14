import {BrowserModule} from '@angular/platform-browser';
import {AppComponent} from './app.component';
import {NavigationComponent} from "./navigation/navigation.component";
import {AppRoutingModule} from "./app-routing.module";
import {NgbModule} from "@ng-bootstrap/ng-bootstrap";
import {CommonDataService} from "./services/CommonDataService";
import {NgModule} from "@angular/core";
import {HttpClientModule} from "@angular/common/http";
import {FormsModule} from "@angular/forms";
import {registerLocaleData} from "@angular/common";
import localeDe from '@angular/common/locales/de'

registerLocaleData(localeDe);

@NgModule({
  declarations: [
    AppComponent,
    NavigationComponent
  ],
  imports: [
    BrowserModule.withServerTransition({appId: 'ng-cli-universal'}),
    HttpClientModule,
    FormsModule,
    AppRoutingModule,
    NgbModule
  ],
  providers: [CommonDataService],
  bootstrap: [AppComponent]
})
export class AppModule {
}
