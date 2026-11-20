import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Stick } from '../models/inventory.models';
import { InventoryService } from '../services/inventory.service';

@Component({
  selector: 'app-stick-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stick-list.component.html',
  styleUrl: './stick-list.component.css'
})
export class StickListComponent implements OnInit {
  availableSticks: Stick[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(private inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.inventoryService.getSticks().subscribe({
      next: data => {
        // StatusStick.Available bernilai 0 pada enum backend.
        this.availableSticks = data.filter(stick => stick.statusStick === 0);
        this.isLoading = false;
      },
      error: error => {
        console.error('Gagal mengambil daftar stik:', error);
        this.errorMessage = 'Daftar stik gagal dimuat.';
        this.isLoading = false;
      }
    });
  }
}