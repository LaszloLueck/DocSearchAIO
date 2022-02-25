import {Component, OnInit} from '@angular/core';
import {CommonDataService} from "../services/CommonDataService";
import {Subscription} from "rxjs";

@Component({
  selector: 'app-navigation',
  templateUrl: './navigation.component.html',
  styleUrls: ['./navigation.component.scss']
})
export class NavigationComponent implements OnInit {
  foo!: Subscription;
  title!: string;
  progressVisible: boolean = false;
  progressValue: number = 0;

  constructor(private commonDataService: CommonDataService) {

  }

  ngOnDestroy(): void {
    this.commonDataService.clearData();
    this.foo.unsubscribe();
  }


  ngOnInit(): void {
    this.foo = this.commonDataService.getData().subscribe(d => this.title = d);
  }
}
