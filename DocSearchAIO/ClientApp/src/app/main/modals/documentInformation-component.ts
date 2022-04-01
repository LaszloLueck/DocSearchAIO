import {Component, Input} from "@angular/core";
import {NgbActiveModal, NgbModal} from "@ng-bootstrap/ng-bootstrap";
import {DocumentdetailService} from "../services/documentdetail.service";
import {DocumentDetailRequest} from "../interfaces/DocumentDetailRequest";
import {Observable} from "rxjs";
import {DocumentDetailResponse} from "../interfaces/DocumentDetailResponse";


@Component({
  selector: 'documentInformation-modal-content',
  templateUrl: './documentInformation-component.template.html'
})
export class DocumentInformationContent {
  @Input() informationId!: string;
  closed: boolean = false;
  documentDetailResponse!: Observable<DocumentDetailResponse>;
  constructor(public activeModal: NgbActiveModal) {
  }
}


@Component({
  selector: 'documentInformation-modal-component',
  templateUrl: './documentInformation-component.html'
})
export class DocumentInformationComponent {
  @Input() informationId!: string;

  constructor(private modalService: NgbModal, private documentDetailService: DocumentdetailService) {}

  open():void {
    const modalRef = this.modalService.open(DocumentInformationContent, {size: 'xl'});
    modalRef.componentInstance.informationId = this.informationId;
    modalRef.componentInstance.documentDetailResponse = this.fetchData(this.informationId);
  }


  fetchData(documentId: string): Observable<DocumentDetailResponse>{
    const request: DocumentDetailRequest = {
      id: documentId
    }
    console.log(documentId);
    return this.documentDetailService.documentDetail(request);

  }

}
