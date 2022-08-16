import {Component, Input, OnInit} from '@angular/core';

@Component({
  selector: 'app-detail',
  templateUrl: './detail.component.html',
  styleUrls: ['./detail.component.scss']
})
export class DetailComponent implements OnInit {

  constructor() {
  }

  @Input() public propertyName!: string;
  @Input() public propertyValue!: string;
  @Input() public lastElement: boolean = false;
  public clList = ['input-group', 'input-group-sm', 'ps-2', 'pt-2', 'pe-2'];
  public cListEnd = ['input-group', 'input-group-sm', 'ps-2', 'pt-2', 'pe-2', 'pb-2']

  renderClassList(): string {
    if(this.lastElement)
      return this.cListEnd.join(' ');

    return this.clList.join(' ');
  }


  ngOnInit(): void {

  }

}
