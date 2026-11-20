import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RoomType } from '../../../models/inventory.models';
import { InventoryService } from '../../../services/inventory/inventory.service';

@Component({
  selector: 'app-room-type-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './room-type-list.component.html',
  styleUrl: './room-type-list.component.css'
})
export class RoomTypeListComponent implements OnInit {
  roomTypes: RoomType[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(private inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.inventoryService.getRoomTypes().subscribe({
      next: data => {
        this.roomTypes = data;
        this.isLoading = false;
      },
      error: error => {
        console.error('Gagal mengambil jenis ruangan:', error);
        this.errorMessage = 'Data jenis ruangan gagal dimuat.';
        this.isLoading = false;
      }
    });
  }
}