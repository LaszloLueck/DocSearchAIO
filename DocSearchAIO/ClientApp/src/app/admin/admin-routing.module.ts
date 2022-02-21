import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import {ConfigComponent} from "./config/config.component";
import {SchedulerComponent} from "./scheduler/scheduler.component";
import {StatisticsComponent} from "./statistics/statistics.component";
import {ActionsComponent} from "./actions/actions.component";

const routes: Routes = [
  {path: 'config', component: ConfigComponent, data: {title: 'Konfiguration'}},
  {path: 'scheduler', component: SchedulerComponent, data: {title: 'Scheduler'}},
  {path: 'statistics', component: StatisticsComponent, data: {title: 'Statistiken'}},
  {path: 'actions', component: ActionsComponent, data: {title: 'Aktionen'}}
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule { }
