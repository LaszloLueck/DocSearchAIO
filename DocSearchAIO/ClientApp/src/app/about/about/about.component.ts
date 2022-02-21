import {Component, OnInit} from '@angular/core';
import {CommonDataService} from "../../services/CommonDataService";

@Component({
  selector: 'app-about',
  templateUrl: './about.component.html',
  styleUrls: ['./about.component.scss']
})
export class AboutComponent implements OnInit {
  constructor(private commonDataService: CommonDataService) { }

  ngOnInit(): void {
    this.commonDataService.sendData("About")
  }

}
