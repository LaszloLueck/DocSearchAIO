import {Component, Input, OnInit} from '@angular/core';
import {NavigationResult} from "../interfaces/SearchResponse";

@Component({
  selector: 'app-pagination',
  templateUrl: './pagination.component.html',
  styleUrls: ['./pagination.component.scss']
})
export class PaginationComponent implements OnInit {
  @Input() navigation!: NavigationResult;

  // documentCount: number = 0;
  // pageSize: number = 50;
  // currentPage: number = 1;
  // searchPhrase: string = "";

  getNgForCounter(count: number): number[] {
    return new Array(count);
  }

  getCurrentPageNumber(): number {
    return this.navigation.currentPageSize === 0 ? 1 : Math.round(this.navigation.currentPage / this.navigation.currentPageSize + 1);
  }

  getModResult():number {
    return (this.navigation.docCount % this.navigation.currentPageSize === 0) ? 0 : 1;
  }

  getPagingCount(): number {
    return this.navigation.docCount <= this.navigation.currentPageSize ? 0 : Math.round((this.navigation.docCount - this.navigation.docCount % this.navigation.currentPageSize) / this.navigation.currentPageSize) + this.getModResult();
  }

  constructor() {
  }

  ngOnInit(): void {
    if(this.navigation){
      console.log("Navigation:" + this.navigation)
    }
  }

}
