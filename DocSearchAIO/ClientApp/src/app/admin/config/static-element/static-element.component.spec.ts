import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StaticElementComponent } from './static-element.component';

describe('StaticElementComponent', () => {
  let component: StaticElementComponent;
  let fixture: ComponentFixture<StaticElementComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ StaticElementComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(StaticElementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
