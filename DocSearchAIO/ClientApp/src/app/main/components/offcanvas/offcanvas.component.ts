import {Component, Input, OnInit} from '@angular/core';
import {LocalStorageDataset} from "../../interfaces/LocalStorageDataset";

@Component({
  selector: 'app-offcanvas',
  templateUrl: './offcanvas.component.html',
  styleUrls: ['./offcanvas.component.scss']
})
export class OffcanvasComponent implements OnInit {
  isOpen: boolean = false;
  @Input() localStorageDataSet!: LocalStorageDataset;

  constructor() { }

  ngOnInit(): void {
  }

  openNav(): void {
    if(!this.isOpen) {
      if (document.getElementById('mySidenav')) {
        document.getElementById("mySidenav")!.classList.add("sidenav-option-in");
        document.getElementById("mySidenav")!.classList.remove("sidenav-option-out")
      }

      if(document.getElementById("fader"))
        document.getElementById("fader")!.className = "fade-in";

      if (document.getElementById("main"))
        document.getElementById("main")!.style.marginLeft = "15%";
    }

    this.isOpen = true;

  }

  closeNav(): void {
    if(document.getElementById("mySidenav")) {
      document.getElementById("mySidenav")!.classList.add("sidenav-option-out");
      document.getElementById("mySidenav")!.classList.remove("sidenav-option-in");
    }
    if(document.getElementById("fader"))
      document.getElementById("fader")!.className="fade-out";

    if(document.getElementById("main"))
      document.getElementById("main")!.style.marginLeft = "0";

    this.isOpen = false;
  }

}
