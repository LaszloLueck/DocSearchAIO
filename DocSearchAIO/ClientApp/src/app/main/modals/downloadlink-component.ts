import {Component, Input} from "@angular/core";
import {NgbActiveModal, NgbModal} from "@ng-bootstrap/ng-bootstrap";
import {Clipboard} from "@angular/cdk/clipboard";


@Component({
  selector: 'downloadlink-modal-content',
  templateUrl: './downloadlink-component.template.html'
})
export class DownloadlinkContent {
  @Input() link!: string;
  @Input() fileType!: string;
  constructor(public activeModal: NgbActiveModal, private clipboard: Clipboard) {}

  downloadFile(fileUrl: string, fileType: string): void {
    const downloadLink = `https://localhost:7299/api/base/download?path=${fileUrl}&documentType=${fileType}`;
    window.open(downloadLink, '_blank');
  }

  copyToClipBoard(): void {
    this.clipboard.copy(this.link);
  }

}

@Component({
  selector: 'downloadlink-modal-component',
  templateUrl: './downloadlink-component.html'
})
export class DownloadLinkComponent {
  @Input() link!: string;
  @Input() fileType!: string;
  constructor(private modalService: NgbModal) {}

  open():void {
    const modalRef = this.modalService.open(DownloadlinkContent, {size: 'lg'});
    modalRef.componentInstance.link = this.link;
    modalRef.componentInstance.fileType = this.fileType;
  }

}
