import {Component, OnInit} from '@angular/core';

@Component({
  selector: 'app-pagination',
  templateUrl: './pagination.component.html',
  styleUrls: ['./pagination.component.scss']
})
export class PaginationComponent implements OnInit {
  documentCount: number = 0;
  pageSize: number = 50;
  currentPage: number = 1;
  searchPhrase: string = "";

  getNgForCounter(count: number): number[] {
    return new Array(count);
  }

  getCurrentPageNumber(): number {
    return this.pageSize === 0 ? 1 : Math.round(this.currentPage / this.pageSize + 1);
  }

  getModResult():number {
    return (this.documentCount % this.pageSize === 0) ? 0 : 1;
  }

  getPagingCount(): number {
    return this.documentCount <= this.pageSize ? 0 : Math.round((this.documentCount - this.documentCount % this.pageSize) / this.pageSize) + this.getModResult();
  }

  constructor() {
  }

  ngOnInit(): void {
  }

}
