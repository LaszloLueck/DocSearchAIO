import {Component} from "@angular/core";
import {NgbActiveModal, NgbModal} from "@ng-bootstrap/ng-bootstrap";

@Component({
  selector: 'optionmodal-content',
  templateUrl: './optionmodal-component.template.html'
})
export class OptionModalContent {

  constructor(public activeModal: NgbActiveModal) {
  }

}

@Component({
  selector: 'optionmodal-component',
  templateUrl: './optionmodal-component.html'
})
export class OptionModalComponent {

  constructor(private modalService: NgbModal) {
  }

  open():void {
    const modalRef = this.modalService.open(OptionModalContent, {size: 'xl'});
    modalRef.componentInstance.content

  }

}
