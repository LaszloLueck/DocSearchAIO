import {Component, ElementRef, Input, OnInit, ViewChild} from '@angular/core';
import {MatAutocomplete} from "@angular/material/autocomplete";

@Component({
  selector: 'app-suggestion',
  templateUrl: './suggestion.component.html',
  styleUrls: ['./suggestion.component.scss']
})
export class SuggestionComponent implements OnInit {


  constructor() { }

  escapePressed(): void {
    console.log("ESCAPE pressed!");
  }

  keyPressed(value: string): void {
    console.log("value is: " + value);
  }

  ngOnInit(): void {
  }

}
