import { Component, OnInit } from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";
import {NavigationResult, SearchStatistic} from "../interfaces/SearchResponse";

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit {
  docCount!: number;
  statistic!: SearchStatistic;
  navigation!: NavigationResult;

  constructor(private commonDataService: CommonDataService) { }

  handleStatistic(eventHandler: SearchStatistic){
    this.statistic = eventHandler;
  }

  handleNavigation(eventHandler: NavigationResult){
    this.navigation = eventHandler;
  }

  ngOnInit(): void {
    this.commonDataService.sendData('Startseite');
  }

}
