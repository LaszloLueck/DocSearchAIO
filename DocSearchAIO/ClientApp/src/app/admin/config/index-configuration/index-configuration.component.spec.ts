import { ComponentFixture, TestBed } from '@angular/core/testing';

import { IndexConfigurationComponent } from './index-configuration.component';

describe('IndexConfigurationComponent', () => {
  let component: IndexConfigurationComponent;
  let fixture: ComponentFixture<IndexConfigurationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ IndexConfigurationComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(IndexConfigurationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
