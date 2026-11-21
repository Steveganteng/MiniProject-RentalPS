import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ConsoleUnit } from '../models/inventory.models';
import { InventoryService } from '../services/inventory.service';

@Component({
  selector: 'app-console-unit-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './console-unit-list.component.html',
  styleUrl: './console-unit-list.component.css'
})
export class ConsoleUnitListComponent implements OnInit {
  availableUnits: ConsoleUnit[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(private inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.inventoryService.getConsoleUnits().subscribe({
      next: data => {
        this.availableUnits = data.filter(
          unit => unit.statusConsole === 'Available'
        );
        this.isLoading = false;
      },
      error: error => {
        console.error('Gagal mengambil unit PlayStation:', error);
        this.errorMessage = 'Daftar unit PlayStation gagal dimuat.';
        this.isLoading = false;
      }
    });
  }
}