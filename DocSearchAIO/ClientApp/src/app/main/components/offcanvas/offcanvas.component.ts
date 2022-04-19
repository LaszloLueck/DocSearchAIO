import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-offcanvas',
  templateUrl: './offcanvas.component.html',
  styleUrls: ['./offcanvas.component.scss']
})
export class OffcanvasComponent implements OnInit {
  isOpen: boolean = false;


  constructor() { }

  ngOnInit(): void {
  }

  openNav(): void {
    if(!this.isOpen) {
      if (document.getElementById('mySidenav')) {
        document.getElementById("mySidenav")!.style.width = "15%";
        document.getElementById("mySidenav")!.style.boxShadow = "10px 0 15px 0 #bebebe";
        document.getElementById("mySidenav")!.style.borderRight = "#bebebe 1px solid";
      }
      if (document.getElementById("main"))
        document.getElementById("main")!.style.marginLeft = "15%";
    }

    this.isOpen = true;

  }

  closeNav(): void {
    if(document.getElementById("mySidenav")) {
      document.getElementById("mySidenav")!.style.width = "0";
      document.getElementById("mySidenav")!.style.boxShadow = "none";
      document.getElementById("mySidenav")!.style.borderRight = "none";
    }
    if(document.getElementById("main"))
      document.getElementById("main")!.style.marginLeft = "0";

    this.isOpen = false;
  }

}
