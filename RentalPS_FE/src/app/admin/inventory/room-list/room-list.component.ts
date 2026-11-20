import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Room } from '../../../models/inventory.models';
import { InventoryService } from '../../../services/inventory/inventory.service';

@Component({
  selector: 'app-room-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './room-list.component.html',
  styleUrl: './room-list.component.css'
})
export class RoomListComponent implements OnInit {
  rooms: Room[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(private inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.inventoryService.getRooms().subscribe({
      next: data => {
        this.rooms = data;
        this.isLoading = false;
      },
      error: error => {
        console.error('Gagal mengambil daftar ruangan:', error);
        this.errorMessage = 'Daftar ruangan gagal dimuat.';
        this.isLoading = false;
      }
    });
  }
}