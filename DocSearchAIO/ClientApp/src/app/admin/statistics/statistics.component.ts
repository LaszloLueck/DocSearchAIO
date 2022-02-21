import { Component, OnInit } from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";

@Component({
  selector: 'app-statistics',
  templateUrl: './statistics.component.html',
  styleUrls: ['./statistics.component.scss']
})
export class StatisticsComponent implements OnInit {

  constructor(private commonDataService: CommonDataService) { }

  ngOnInit(): void {
    this.commonDataService.sendData('Statistiken');
  }

}
