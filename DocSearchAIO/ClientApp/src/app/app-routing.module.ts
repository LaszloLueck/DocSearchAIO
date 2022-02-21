import {NgModule} from '@angular/core';
import {RouterModule, Routes} from "@angular/router";

const appRoutes: Routes = [
  {path: 'home', loadChildren: () => import('./main/main.module').then(m => m.MainModule) },
  {path: 'about', loadChildren: () => import('./about/about.module').then(m => m.AboutModule)},
  {path: 'admin', loadChildren: () => import('./admin/admin.module').then(m => m.AdminModule)},
  {path: '', redirectTo: 'home', pathMatch: 'full'}
]

@NgModule({
  declarations: [],
  imports: [
    RouterModule.forRoot(appRoutes)
  ],
  exports: [RouterModule]
})
export class AppRoutingModule { }
