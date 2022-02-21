import { Component, OnInit } from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit {

  constructor(private commonDataService: CommonDataService) { }

  ngOnInit(): void {
    this.commonDataService.sendData('Startseite');
  }

}
