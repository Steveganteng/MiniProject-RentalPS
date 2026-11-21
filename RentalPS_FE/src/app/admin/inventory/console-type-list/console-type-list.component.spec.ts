import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConsoleTypeListComponent } from './console-type-list.component';

describe('ConsoleTypeListComponent', () => {
  let component: ConsoleTypeListComponent;
  let fixture: ComponentFixture<ConsoleTypeListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConsoleTypeListComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ConsoleTypeListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
