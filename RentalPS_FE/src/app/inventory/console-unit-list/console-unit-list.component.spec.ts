import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConsoleUnitListComponent } from './console-unit-list.component';

describe('ConsoleUnitListComponent', () => {
  let component: ConsoleUnitListComponent;
  let fixture: ComponentFixture<ConsoleUnitListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConsoleUnitListComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ConsoleUnitListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
