import { Component, OnInit } from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";

@Component({
  selector: 'app-scheduler',
  templateUrl: './scheduler.component.html',
  styleUrls: ['./scheduler.component.scss']
})
export class SchedulerComponent implements OnInit {

  constructor(private commonDataService: CommonDataService) { }

  ngOnInit(): void {
    this.commonDataService.sendData('Scheduler');
  }

}
