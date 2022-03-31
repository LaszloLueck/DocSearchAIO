import {Component, Input, OnInit, Pipe, PipeTransform, ViewChild} from "@angular/core";
import {NgbAlert} from "@ng-bootstrap/ng-bootstrap";


@Component({
  selector: 'ngbd-alert-selfclosing',
  templateUrl: './alert-selfclosing.html'})
export class NgbdAlertSelfclosing implements OnInit{
  @Input() message!: string;
  @Input() timeout: number = 5000;
  @ViewChild('selfClosingAlert', {static: false}) selfClosingAlert!: NgbAlert;

  ngOnInit(): void {
    setTimeout(() => this.selfClosingAlert.close(), this.timeout);
  }
}
