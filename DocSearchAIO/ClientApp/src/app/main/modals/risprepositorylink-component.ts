import {Component, Input} from "@angular/core";
import {NgbActiveModal, NgbModal} from "@ng-bootstrap/ng-bootstrap";
import {Clipboard} from "@angular/cdk/clipboard";

@Component({
  selector: 'risprepositorylink-modal-content',
  templateUrl: './risprepositorylink-component.template.html'
})
export class RisprepositorylinkContent {
  @Input() content!: string;

  constructor(public activeModal: NgbActiveModal, private clipboard: Clipboard) {}

  copyToClipBoard(): void {
    this.clipboard.copy(this.content);
  }
}

@Component({
  selector: 'risprepositorylink-modal-component',
  templateUrl: './risprepositorylink-component.html'
})
export class RisprepositorylinkComponent {
  @Input() content!: string;
  constructor(private modalService: NgbModal) {}

  open():void {
    const modalRef = this.modalService.open(RisprepositorylinkContent, {size: 'lg'});
    modalRef.componentInstance.content = this.content;
  }
}
