import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { DashboardService } from '../../services/dashboard/dashboard.service';
interface SummaryCard {
  title: string;
  value: string | number | null;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  summaryCards: SummaryCard[] = [
    {title: 'Unit Console', value:'_'},
    {title: 'Ruangan', value:'_'},
    {title: 'Stik', value:'_'},
    {title: 'Rental Aktif', value:'_'}];

    constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.dashboardService.getAvailableUnits().subscribe({
      next: units => {
        const unitCard = this.summaryCards.find(
          card => card.title === 'Unit Console'
        );

        if (unitCard) {
          unitCard.value = units.length;
        }
      },
      error: error => {
        console.error('Gagal mengambil unit console:', error);
      }
    });
  }
}