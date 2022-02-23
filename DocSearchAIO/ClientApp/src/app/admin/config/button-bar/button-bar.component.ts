import {Component, Input, OnInit} from '@angular/core';
import {Router} from "@angular/router";

@Component({
  selector: 'app-button-bar',
  templateUrl: './button-bar.component.html',
  styleUrls: ['./button-bar.component.scss']
})
export class ButtonBarComponent implements OnInit {
  @Input() invalidForm!: boolean;

  constructor(private router: Router) { }

  doCancel(): void {
    this.router.navigate(['/home']);
  }

  ngOnInit(): void {
  }

}
