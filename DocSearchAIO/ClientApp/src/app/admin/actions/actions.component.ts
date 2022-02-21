import { Component, OnInit } from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";

@Component({
  selector: 'app-actions',
  templateUrl: './actions.component.html',
  styleUrls: ['./actions.component.scss']
})
export class ActionsComponent implements OnInit {

  constructor(private commonDataService: CommonDataService) { }

  ngOnInit(): void {
    this.commonDataService.sendData('Aktionen');
  }

}
