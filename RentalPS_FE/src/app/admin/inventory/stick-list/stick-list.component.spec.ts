import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StickListComponent } from './stick-list.component';

describe('StickListComponent', () => {
  let component: StickListComponent;
  let fixture: ComponentFixture<StickListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StickListComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(StickListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
