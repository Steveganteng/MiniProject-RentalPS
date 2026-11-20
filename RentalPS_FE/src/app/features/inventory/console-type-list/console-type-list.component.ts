import { CommonModule, CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { AdminInventoryService, ConsoleTypeItem } from '../../../services/admin/admin-inventory.service';

@Component({
  selector: 'app-console-type-list',
  standalone: true,
  imports: [CommonModule, CurrencyPipe],
  templateUrl: './console-type-list.component.html',
  styleUrl: './console-type-list.component.css'
})
export class ConsoleTypeListComponent implements OnInit {
  private readonly inventoryService = inject(AdminInventoryService);

  consoleTypes: ConsoleTypeItem[] = [];
  errorMessage = '';

  ngOnInit(): void {
    this.inventoryService.getConsoleTypes().subscribe({
      next: items => this.consoleTypes = items,
      error: error => this.errorMessage = error?.error?.message || 'Tipe konsol gagal dimuat.'
    });
  }
}
