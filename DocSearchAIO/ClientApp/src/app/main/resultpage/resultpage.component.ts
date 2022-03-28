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

  openDetailModal(id:string){
    console.log(id);
  }

  getFileName(path: string): string {
    return path.substring(path.lastIndexOf("/") + 1);
  }

  ngOnInit(): void {
  }

}
