import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-stats-counter',
  templateUrl: './stats-counter.component.html',
  styleUrls: ['./stats-counter.component.scss']
})
export class StatsCounterComponent implements OnInit {
  docCount: number = 0;
  searchTime: number = 0;
  isVisible: boolean = false;
  constructor() { }

  ngOnInit(): void {
  }

}
