import {Component, Input, OnInit} from '@angular/core';
import {SearchStatistic} from "../interfaces/SearchResponse";

@Component({
  selector: 'app-stats-counter',
  templateUrl: './stats-counter.component.html',
  styleUrls: ['./stats-counter.component.scss']
})
export class StatsCounterComponent implements OnInit {
  @Input() statistic!: SearchStatistic
  constructor() { }

  ngOnInit(): void {
  }

}
