import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ConsoleType } from '../../models/inventory.models';
import { InventoryService } from '../../services/inventory/inventory.service';

@Component({
  selector: 'app-console-type-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './console-type-list.component.html',
  styleUrl: './console-type-list.component.css'
})
export class ConsoleTypeListComponent implements OnInit {
  consoleTypes: ConsoleType[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(private inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.inventoryService.getConsoleTypes().subscribe({
      next: data => {
        this.consoleTypes = data;
        this.isLoading = false;
      },
      error: error => {
        console.error('Gagal mengambil tipe PS:', error);
        this.errorMessage = 'Data tipe PS gagal dimuat.';
        this.isLoading = false;
      }
    });
  }
}