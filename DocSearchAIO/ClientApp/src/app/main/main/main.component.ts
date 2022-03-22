import { Component, OnInit } from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";
import {SearchStatistic} from "../interfaces/SearchResponse";

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit {
  docCount!: number;
  statistic!: SearchStatistic;

  constructor(private commonDataService: CommonDataService) { }

  handleStatistic(eventHandler: SearchStatistic){
    console.log("E: " + eventHandler)
    this.statistic = eventHandler;
  }


  ngOnInit(): void {
    this.commonDataService.sendData('Startseite');
  }

}
