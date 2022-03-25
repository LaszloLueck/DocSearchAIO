import {Component, Input, OnInit} from '@angular/core';
import {SearchResult} from "../interfaces/SearchResponse";

@Component({
  selector: 'app-resultpage',
  templateUrl: './resultpage.component.html',
  styleUrls: ['./resultpage.component.scss']
})
export class ResultpageComponent implements OnInit {
  @Input() searchResult!: SearchResult;

  constructor() { }

  ngOnInit(): void {
  }

}
