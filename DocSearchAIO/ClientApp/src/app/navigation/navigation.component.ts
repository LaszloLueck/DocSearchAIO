import {CommonDataService} from "../services/CommonDataService";
import {Subscription} from "rxjs";
import {Component, OnInit} from "@angular/core";

@Component({
  selector: 'app-navigation',
  templateUrl: './navigation.component.html',
  styleUrls: ['./navigation.component.scss']
})
export class NavigationComponent implements OnInit {
  subscription!: Subscription;
  title!: string;

  constructor(private commonDataService: CommonDataService) {

  }

  ngOnDestroy(): void {
    this.commonDataService.clearData();
    this.subscription.unsubscribe();
  }


  ngOnInit(): void {
    this.subscription = this.commonDataService.getData().subscribe(d => this.title = d);
  }
}
