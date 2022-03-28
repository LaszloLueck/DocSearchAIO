import {Component, Input} from "@angular/core";
import {NgbActiveModal, NgbModal} from "@ng-bootstrap/ng-bootstrap";


@Component({
  selector: 'documentInformation-modal-content',
  templateUrl: './documentInformation-component.template.html'
})
export class DocumentInformationContent {
  @Input() informationId!: string;

  constructor(public activeModal: NgbActiveModal) {
  }

}


@Component({
  selector: 'documentInformation-modal-component',
  templateUrl: './documentInformation-component.html'
})
export class DocumentInformationComponent {

  @Input() informationId!: string;

  constructor(private modalService: NgbModal) {}

  open():void {
    const modalRef = this.modalService.open(DocumentInformationContent, {size: 'lg'});
    modalRef.componentInstance.informationId = this.informationId;
  }

}
