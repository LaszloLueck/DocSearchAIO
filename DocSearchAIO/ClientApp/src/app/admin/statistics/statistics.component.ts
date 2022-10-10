import {Component, OnDestroy, OnInit} from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";
import {IndexStatisticsService} from "./services/index-statistics.service";
import {Subscription} from "rxjs";
import {IndexStatisticsResponse} from "../config/interfaces/IndexStatisticsResponse";
import {AlternateReturn} from "../config/interfaces/AlternateReturn";
import {Either, match} from "../../generic/either";
import {BaseError} from "../config/interfaces/DocSearchConfiguration";

@Component({
  selector: 'app-statistics',
  templateUrl: './statistics.component.html',
  styleUrls: ['./statistics.component.scss']
})
export class StatisticsComponent implements OnInit, OnDestroy {

  private subscription!: Subscription;
  public data!: IndexStatisticsResponse;
  public alternateReturn!: AlternateReturn;

  constructor(private commonDataService: CommonDataService, private indexStatisticsService: IndexStatisticsService) { }

  ngOnInit(): void {
    this.commonDataService.sendData('Statistiken');
    this.loadData();
  }

  ngOnDestroy(): void {
    if(this.subscription)
      this
        .subscription
        .unsubscribe();
  }

  loadData(): void {
    this.subscription = this
      .indexStatisticsService
      .getIndexcStatisticsData()
      .subscribe((data: Either<BaseError, IndexStatisticsResponse>) => {
        match(
          data,
          left => this.alternateReturn = left,
          right => this.data = right
        )
      });
  }

}
